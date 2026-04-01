using Express.Logic.Kalkulatory.LTR.CalculatorReport;
using Express.WD;
using System;
using System.Linq;
using TF.BusinessFramework.Patterns.UnitOfWork;

namespace Express.Logic.Kalkulatory.LTR
{
    public class LTRSubCalculatorOpony : LTRSubCalculator
    {
        public class Data : LTRSubCalculatorData
        {
        }

        public class Result : LTRSubCalculatorResult
        {
            public decimal OponyNetto { get; set; }
            public decimal Koszt1KplOpon { get; set; }
            public decimal IloscOpon { get; set; }
        }

        public LTRSubCalculatorOpony(Kalkulacja kalkulacja, LTRTabele tabele) : base(kalkulacja, tabele)
        {

        }

        public Result Policz(Data data)
        {

            bool oponyWielosezonowe = _kalkulacja.KlasaOpon != null && _kalkulacja.KlasaOpon.StartsWith(Express.Types.Opony.OponyKlasaConst.WIELOSEZONOWE);

            decimal liczbaLat = ((decimal)_tabele.Okres) / 12.0m;

            // Koszt kompletu opon
            decimal koszt1kompletu = getKoszt1KompletuOpon();

            // Ilosć opon na kontrakt
            decimal iloscOponNaKontrakt = getIloscOponNaKontrakt(oponyWielosezonowe);
            
            decimal cenaPrzekladkinNet = 0;
            decimal przechowywanieOponNet = 0;
            decimal lacznyKosztOpon = 0;
            decimal odkupOpon = 0;
            decimal oponyNetto = 0;

            if (_kalkulacja.ZOponami.IsTrue() && iloscOponNaKontrakt > 0)
            {
                cenaPrzekladkinNet = getCenaPrzekladkiNetto(liczbaLat, oponyWielosezonowe);
                przechowywanieOponNet = getKosztPrzechowywaniaOponNetto(liczbaLat, oponyWielosezonowe);
                lacznyKosztOpon = getLacznyKosztOpon(oponyWielosezonowe, iloscOponNaKontrakt, koszt1kompletu);
                odkupOpon = GetOdkupOponKwota();

                oponyNetto = lacznyKosztOpon + cenaPrzekladkinNet + przechowywanieOponNet - odkupOpon;
            }

            if (_tabele.IsReport)
            {


                CalcReportNameValueTable table = new CalcReportNameValueTable("OPONY");
                _tabele.Report.AddItem(table);
                table.AddValue("iloscOponNaKontrakt", iloscOponNaKontrakt);
                table.AddValue("koszt1kompletu", koszt1kompletu);
                table.AddValue("lacznyKosztOpon", lacznyKosztOpon);
                table.AddValue("cenaPrzekladki", cenaPrzekladkinNet);
                table.AddValue("przechowywanieOpon", przechowywanieOponNet);
                table.AddValue("odkupOpon", odkupOpon);
                table.AddValue("opony", oponyNetto, true);
            }

            return new Result()
            {
                OponyNetto = oponyNetto,
                Koszt1KplOpon = koszt1kompletu,
                IloscOpon = iloscOponNaKontrakt
            };

        }

        decimal getCenaPrzekladkiNetto(decimal liczbaLat, bool oponyWielosezonowe)
        {
            decimal cenaPrzekladkiNetto = Kwota.GetAsDecimal(_tabele.Parametry.OponyPrzekladki);

            if (oponyWielosezonowe)
                return Math.Ceiling(_tabele.Przebieg / 60000.0m) * cenaPrzekladkiNetto;
            else
                return cenaPrzekladkiNetto * liczbaLat * 2;
        }

        decimal getKosztPrzechowywaniaOponNetto(decimal liczbaLat, bool oponyWielosezonowe)
        {
            if (oponyWielosezonowe)
                return 0m;
            else
                return (Kwota.GetAsDecimal(_tabele.Parametry.OponyPrzechowywane)) * liczbaLat * 2;
        }

        decimal GetOdkupOponKwota()
        {
            if (_kalkulacja.ZOponami.IsFalse()) return 0;

            if (_kalkulacja.OdkupOpon.IsFalse()) return 0;

            if (null == _kalkulacja.RozmiarOpon)
                throw new Exception("Brak RozmiarOpon w Kalkulacji");

            PozycjaCennikaOpon pozycja = _tabele.iUnitOfWork.GetRepository<PozycjaCennikaOpon>().AsQueryable()
              .Where(pc => pc.CennikOponId == _tabele.CennikOponId && pc.Rozmiar.Srednica == _kalkulacja.RozmiarOpon.Srednica && pc.KlasaOpon == _kalkulacja.KlasaOpon && pc.ModelOpon.MarkaOpon.Nazwa == "Odkup opon")
              .FirstOrDefault();

            if (null == pozycja)
                throw new Exception("Brak pozycji dla odkupu opon w tabeli PozycjaCennikaOpons");

            return Kwota.GetAsDecimal(pozycja.Netto);
        }

        decimal getKoszt1KompletuOpon()
        {

            if (_kalkulacja.ZOponami.IsFalse()) return 0;

            if (null == _kalkulacja.RozmiarOpon)
                throw new Exception("Brak RozmiarOpon w Kalkulacji");

            decimal koszt1kompletu = 0m;
            
            PozycjaCennikaOpon pozycja = _tabele.iUnitOfWork.GetRepository<PozycjaCennikaOpon>().AsQueryable()
                .Where(pc => pc.CennikOponId == _tabele.CennikOponId && pc.Rozmiar.Srednica == _kalkulacja.RozmiarOpon.Srednica && pc.KlasaOpon == _kalkulacja.KlasaOpon && pc.ModelOpon.MarkaOpon.Nazwa == "Budżet")
                .FirstOrDefault();

            if (null == pozycja)
                throw new Exception("Brak pozycji w tabeli PozycjaCennikaOpons");

            koszt1kompletu = Kwota.GetAsDecimal(pozycja.Netto);

            if (_kalkulacja.KorektaKosztuOpon.IsTrue())
                koszt1kompletu += (Kwota.GetAsDecimal(_kalkulacja.KosztOponKorekta) / _tabele.Parametry.StawkaVAT);

            return koszt1kompletu;

        }

        decimal getIloscOponNaKontrakt(bool oponyWielosezonowe)
        {
            // 0 = automatycznie
            // -1 = polowki

            if (_kalkulacja.ZOponami.IsFalse()) return 0;

            bool isAuto = _kalkulacja.AutoLiczbaOpon.IsTrue() || _kalkulacja.LiczbaKompletowOpon.GetValue() == 0;

            if (!isAuto)
                return _kalkulacja.LiczbaKompletowOpon.GetValue();

            if (oponyWielosezonowe)
            {
                if (_tabele.Przebieg <= 60000)
                    return 1;
                else if (_tabele.Przebieg <= 120000)
                    return 2;
                else if (_tabele.Przebieg <= 180000)
                    return 3;
                else if (_tabele.Przebieg <= 240000)
                    return 4;
                else if (_tabele.Przebieg <= 300000)
                    return 5;
                else 
                    return 6;
            }
            else
            {
                if (_tabele.Przebieg <= 120000)
                    return 1;
                else if (_tabele.Przebieg <= 180000)
                    return 2;
                else if (_tabele.Przebieg <= 240000)
                    return 3;
                else if (_tabele.Przebieg <= 300000)
                    return 4;
                else 
                    return 5;
            }
        }

        decimal getLacznyKosztOpon(bool oponyWielosezonowe, decimal iloscOponNaKontrakt, decimal koszt1kompletu)
        {
            if (_kalkulacja.AutoLiczbaOpon.IsFalse())
                return iloscOponNaKontrakt * koszt1kompletu;

            //to wygląda na jakieś śmieci które globalnie szkodzą - MW wywalam
            //if (_kalkulacja.LiczbaKompletowOpon.GetValue() == 1)
            //    return iloscOponNaKontrakt * koszt1kompletu;

            if (oponyWielosezonowe)
            {
                if (_tabele.Przebieg < 60000)
                    return koszt1kompletu;
                else
                {
                    decimal result = koszt1kompletu;
                    result += ((_tabele.Przebieg - 60000.0m) / 60000.0m) * koszt1kompletu;
                    return result;
                }
            }
            else
            {
                if (_tabele.Przebieg < 120000)
                    return koszt1kompletu;
                else
                {
                    decimal result = koszt1kompletu;
                    result += ((_tabele.Przebieg - 120000.0m) / 60000.0m) * koszt1kompletu;
                    return result;
                }
            }

            
            
           
        }
    }
}
