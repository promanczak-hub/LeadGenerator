using Express.Logic.Kalkulatory.LTR.CalculatorReport;
using Express.Types.Magazyny;
using Express.WD;
using System;
using System.Collections.Generic;
using System.Linq;
using TF.BusinessFramework.Patterns.UnitOfWork;

namespace Express.Logic.Kalkulatory.LTR
{
    public class LTRSubCalculatorKosztyDodatkowe : LTRSubCalculator
    {
        public class Data : LTRSubCalculatorData
        {
        }

        public class Result : LTRSubCalculatorResult
        {
            public decimal KosztyDodatkowe { get; set; }
        }

        public LTRSubCalculatorKosztyDodatkowe(Kalkulacja kalkulacja, LTRTabele tabele) : base(kalkulacja, tabele)
        {
        }

        public Result Policz(Data data)
        {
            

            decimal abonamentGSM = getAbonamentGSM(data);
            decimal przygotowanieDoSprzedazy = CalculatePrzygotowanieDoSprzedazy();
            decimal rejestracjaKartaPojazdu = Kwota.GetAsDecimal(_tabele.Parametry.ZarejestrowanieKartaPojazdu);
            decimal elementyRyczaltowe = GetSumaElementowRyczaltowych();
            decimal kosztyDodatkoweZabudowy = GetKosztyDodatkoweZabudowy();

            decimal demontazKraty = 0;
            bool isKrata = _kalkulacja.isDemontaz.IsTrue();
            if (isKrata)
                demontazKraty = Kwota.GetAsDecimal(_tabele.Parametry.KosztWymontowaniaKraty);

            decimal kosztHakHolowniczy = 0;
            bool isHak = _kalkulacja.Hak.IsTrue();
            if (isHak)
                kosztHakHolowniczy = Kwota.GetAsDecimal(_tabele.Parametry.HakHolowniczy);

            decimal kosztyDodatkowe = przygotowanieDoSprzedazy + rejestracjaKartaPojazdu + demontazKraty + abonamentGSM + elementyRyczaltowe + kosztyDodatkoweZabudowy + kosztHakHolowniczy;

            if (_tabele.IsReport)
            {
                CalcReportNameValueTable table = new CalcReportNameValueTable("KOSZTY DODATKOWE");
                table.AddValue("przygotowanieDoSprzedazyLtr", przygotowanieDoSprzedazy);
                table.AddValue("rejestracjaKartaPojazdu", rejestracjaKartaPojazdu);
                table.AddValue("hakHolowniczy", kosztHakHolowniczy);
                table.AddValue("demontazKraty", demontazKraty);
                table.AddValue("abonamentGSM", abonamentGSM);
                table.AddValue("elementyRyczaltowe", elementyRyczaltowe);
                table.AddValue("kosztyDodatkoweZabudowy", kosztyDodatkoweZabudowy);
                table.AddValue("kosztyDodatkowe", kosztyDodatkowe, true);
                _tabele.Report.AddItem(table);
            }


            return new Result()
            {
                KosztyDodatkowe = kosztyDodatkowe
            };

        }        

        private decimal getAbonamentGSM(Data data)
        {

            if (_kalkulacja.CzyGPS.IsFalse()) return 0;

            LTRAdminGSM pozycja = LTRUtils.Instance.GetBySymbolEurotax<LTRAdminGSM>(_tabele.AbonamentGSM, _kalkulacja);
            if (null == pozycja) throw new Exception("Brak pozycji w tabeli LTRAdminGSM");

            decimal abonament = Kwota.GetAsDecimal(pozycja.Abonament);

            // w arkuszu jest trochę inaczej skontruowany ten warunek, ale wychodzi na to samo
            if (abonament <= 0) return 0;

            int okresUzytkowania = _tabele.Okres;
            decimal cenaUrzadzenia = Kwota.GetAsDecimal(_tabele.Parametry.CenaUrzadzeniaGSM);
            decimal montazUrzadzenia = Kwota.GetAsDecimal(_tabele.Parametry.MontazUrzadzeniaGSM);

            decimal result = abonament * okresUzytkowania + cenaUrzadzenia / 6m * okresUzytkowania / 12m + montazUrzadzenia;
            return result;
        }

        private decimal GetSumaElementowRyczaltowych()
        {
            var result = 0.0m;
            foreach (var element in _kalkulacja.ElementyRyczaltowe.Where(er => er.DefinicjaElementuRyczaltowego.Aktywny))
            {
                if (element.DefinicjaElementuRyczaltowego.StawkaMiesiecznaNetto.HasValue)
                {
                    result += _tabele.Okres * element.DefinicjaElementuRyczaltowego.StawkaMiesiecznaNetto.Value;
                }
                else if (element.DefinicjaElementuRyczaltowego.StawkaZa10TysKmNetto.HasValue)
                {
                    result += (_tabele.Przebieg / 10000.0m) * element.DefinicjaElementuRyczaltowego.StawkaZa10TysKmNetto.Value;
                }
            }
            return result;
        }

        private decimal GetKosztyDodatkoweZabudowy()
        {
            decimal result = 0.0m;

            int? klasaId = _kalkulacja.Model != null ? _kalkulacja.Model.KlasaId : null;
            string typZabudowy = _kalkulacja.TypZabudowy;

            LTRAdminKosztZabudowy adminKosztZabudowy = null;
            if (klasaId.HasValue && !string.IsNullOrEmpty(typZabudowy))
            {
                adminKosztZabudowy = _tabele.KosztZabudowy.Where(k => k.KlasaId == klasaId && k.TypZabudowy == typZabudowy).FirstOrDefault();
            }

            if (adminKosztZabudowy != null)
            {
                result = (_tabele.Okres / 12m) * adminKosztZabudowy.KosztyDodatkoweNettoRok;
            }

            return result;
        }

        private decimal CalculatePrzygotowanieDoSprzedazy()
        {

            decimal result = 1.0m;
            string liniaProduktowa = _kalkulacja.LiniaProduktowa != null ? _kalkulacja.LiniaProduktowa.NazwaLiniiProduktowej : String.Empty;
            if (liniaProduktowa == MagazynyConst.LTR)
            {
                // korekty klasa przebieg tylko dla LTR - #17755 - ewentualnie do wycofania po ustaleniu z B.Guzik
                decimal korektaKlasa = GetKorektaForKlasa();
                decimal korektaPrzebieg = GetKorektaForPrzebieg();
                result = korektaKlasa * korektaPrzebieg * Kwota.GetAsDecimal(_tabele.Parametry.PrzygotowanieDoSprzedazyLtr);
            }
            else
                result *= Kwota.GetAsDecimal(_tabele.Parametry.PrzygotowanieDoSprzedazyRacMtr);

           


            if (_kalkulacja.KorektaKosztuPrzygotowaniaDosprzedazy.IsTrue())
                result += (Kwota.GetAsDecimal(_kalkulacja.KosztPrzygotowaniaDosprzedazyKorekta) / _tabele.Parametry.StawkaVAT);

            return result;

        }

        private decimal GetKorektaForKlasa()
        {
            string klasa = _kalkulacja.Model != null ? _kalkulacja.Model.Klasa != null ? _kalkulacja.Model.Klasa.Nazwa : string.Empty : string.Empty;

            if (klasa == string.Empty)
                throw new Exception("Pobranie korekty niemożliwe, gdyż brak danych dotyczących klasy.");

            LTRAdminKategoriaKorekta korekta = _tabele.KategoriaKorekta.FirstOrDefault(kat => kat.Kategoria == klasa);
            if (korekta == null)
                throw new Exception("Nie znaleziono korekty dla podanej klasy");

            return korekta.WspolczynnikKorektyProcent;
        }

        private decimal GetKorektaForPrzebieg()
        {
            int przebieg = _tabele.Przebieg;
            IEnumerable<LTRAdminPrzebiegKorekta> korektySorted = _tabele.PrzebiegKorekta.OrderByDescending(k => k.Przebieg);
            LTRAdminPrzebiegKorekta korekta = korektySorted.FirstOrDefault(k => k.Przebieg <= przebieg);
            if (korekta == null)
                korekta = korektySorted.LastOrDefault();

            return korekta.WspolczynnikKorektyProcent;
        }
    }
}
