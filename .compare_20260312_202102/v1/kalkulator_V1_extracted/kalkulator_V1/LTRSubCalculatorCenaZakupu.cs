using Express.Logic.Kalkulatory.LTR.CalculatorReport;
using Express.WD;
using System;
using System.Linq;
using TF.BusinessFramework.Patterns.UnitOfWork;

namespace Express.Logic.Kalkulatory.LTR
{
    public class LTRSubCalculatorCenaZakupu : LTRSubCalculator
    {


        public class Data : LTRSubCalculatorData
        {
            public decimal Koszt1KompletOpon { get; set; }
        }


        public class Result : LTRSubCalculatorResult
        {
            public decimal CenaZakupu { get; set; }
            public decimal CenaZakupuBezOpon { get; set; }
            public decimal CenaZakupuBezOponIOpcjiSerwisowych { get; set; }
            public decimal CenaSamochoduBezOpon_OpcjiSerwisowych_iPakietuNetto { get; set; }
            public decimal RabatKwotowo { get; set; }

        }


        public LTRSubCalculatorCenaZakupu(Kalkulacja kalkulacja, LTRTabele tabele)
            : base(kalkulacja, tabele)
        {

        }


        public Result Policz(Data data)
        {

            // UWAGA: ten kalkulator liczy na wartosciach brutto
            if (null == _kalkulacja.CenaCennikowa)
                throw new Exception("Brak pozycji CenaCennikowa w Kalkulacji");


            

            decimal opcjeFabryczneSuma = _kalkulacja.OpcjeFabryczne.Sum(o => Kwota.GetAsDecimal(o.CenaCennikowa));
            decimal cenaKatalogowaZOpcjamiFabrycznymi = Kwota.GetAsDecimal(_kalkulacja.CenaCennikowa) + opcjeFabryczneSuma;

            decimal oplataTransportowaBrutto = getOplataTransportowaBrutto();

            decimal opcjeKatalogoweNierabatowane = _kalkulacja.OpcjeFabryczne
                .Where(o => o.isNierabatowany.IsTrue())
                .Sum(o => Kwota.GetAsDecimal(o.CenaCennikowa));

            decimal cenaKatalogowaZOPcjamiFabrycznymiPoRabacie =
                (cenaKatalogowaZOpcjamiFabrycznymi - oplataTransportowaBrutto - opcjeKatalogoweNierabatowane) *
                (1 - _kalkulacja.RabatProcent)
                + oplataTransportowaBrutto + opcjeKatalogoweNierabatowane;

            decimal rabatKwotowo = cenaKatalogowaZOpcjamiFabrycznymi - cenaKatalogowaZOPcjamiFabrycznymiPoRabacie;

            decimal opcjeSerwisowe = getOpcjeSerwisowe();

            decimal cenaZa1KompletOpon = data.Koszt1KompletOpon * _tabele.Parametry.StawkaVAT;

            decimal pakietSerwisowy = Kwota.GetAsDecimal(_kalkulacja.PakietSerwisowy);

            decimal cenaSamochoduZOponamiIOpcjamiSerwisowymiIPakiet = cenaKatalogowaZOPcjamiFabrycznymiPoRabacie +
                                                               opcjeSerwisowe +
                                                               cenaZa1KompletOpon +
                                                               pakietSerwisowy;

            decimal cenaSamochoduBezOpon = cenaSamochoduZOponamiIOpcjamiSerwisowymiIPakiet - cenaZa1KompletOpon;

            decimal cenaSamochoduBezOponIOpcjiSerwisowych = cenaSamochoduBezOpon - opcjeSerwisowe;

            decimal cenaSamochoduBezOpon_OpcjiSerwisowych_iPakietu = cenaSamochoduBezOponIOpcjiSerwisowych - pakietSerwisowy;


            if (_tabele.IsReport)
            {
                CalcReportNameValueTable table = new CalcReportNameValueTable("CENA ZAKUPU (wartości brutto)");
                _tabele.Report.AddItem(table);
                table.AddValue("opcjeFabryczneSuma", opcjeFabryczneSuma);
                table.AddValue("cenaKatalogowaZOpcjamiFabrycznymi", cenaKatalogowaZOpcjamiFabrycznymi);
                table.AddValue("oplataTransportowa", oplataTransportowaBrutto);
                table.AddValue("opcjeKatalogoweNierabatowane", opcjeKatalogoweNierabatowane);
                table.AddValue("cenaKatalogowaZOPcjamiFabrycznymiPoRabacie", cenaKatalogowaZOPcjamiFabrycznymiPoRabacie);
                table.AddValue("rabatKwotowo", rabatKwotowo);
                table.AddValue("opcjeSerwisowe", opcjeSerwisowe);
                table.AddValue("cenaZa1KompletOpon", cenaZa1KompletOpon);
                table.AddValue("pakietSerwisowy", pakietSerwisowy);
                table.AddValue("cenaSamochoduZOponamiIOpcjamiSerwisowymiIPakiet", cenaSamochoduZOponamiIOpcjamiSerwisowymiIPakiet, true);
                table.AddValue("cenaSamochoduBezOpon", cenaSamochoduBezOpon);
                table.AddValue("cenaSamochoduBezOponIOpcjiSerwisowych", cenaSamochoduBezOponIOpcjiSerwisowych);
                table.AddValue("cenaSamochoduBezOpon_OpcjiSerwisowych_iPakietu", cenaSamochoduBezOpon_OpcjiSerwisowych_iPakietu);
            }

            return new Result()
            {
                CenaZakupu = cenaSamochoduZOponamiIOpcjamiSerwisowymiIPakiet / _tabele.Parametry.StawkaVAT,
                CenaZakupuBezOpon = cenaSamochoduBezOpon / _tabele.Parametry.StawkaVAT,
                CenaZakupuBezOponIOpcjiSerwisowych = cenaSamochoduBezOponIOpcjiSerwisowych / _tabele.Parametry.StawkaVAT,
                CenaSamochoduBezOpon_OpcjiSerwisowych_iPakietuNetto = cenaSamochoduBezOpon_OpcjiSerwisowych_iPakietu / _tabele.Parametry.StawkaVAT,
                RabatKwotowo = rabatKwotowo / _tabele.Parametry.StawkaVAT,
            };

        }

        decimal getOpcjeSerwisowe()
        {


            decimal opcjeSerwisowe = _kalkulacja.OpcjaSerwisowas.Sum(o => Kwota.GetAsDecimal(o.CenaCennikowa));

            if (_kalkulacja.CzyGPS.IsTrue())
            {
                decimal opcjeGSM = 0;
                LTRAdminGSM gsm = LTRUtils.Instance.GetBySymbolEurotax<LTRAdminGSM>(_tabele.AbonamentGSM, _kalkulacja);
                if (null != gsm && Kwota.GetAsDecimal(gsm.Abonament) > 0)
                {
                    opcjeGSM = Kwota.GetAsDecimal(_tabele.Parametry.CenaUrzadzeniaGSM) + Kwota.GetAsDecimal(_tabele.Parametry.MontazUrzadzeniaGSM);
                    opcjeSerwisowe += opcjeGSM * _tabele.Parametry.StawkaVAT;
                }


            }

            return opcjeSerwisowe;
        }




        decimal getOplataTransportowaBrutto()
        {

            int markaId = _kalkulacja.Model.Marka.Id;

            LTRAdminOplatyTransportowe oplata = _tabele.OplatyTransportowe
                .FirstOrDefault(o => o.MarkaId == markaId);


            if (null == oplata)
                return 0.0m;
            //throw new Exception("Brak pozycji w LTRAdminOplatyTransportowe");


            return oplata.OplataBrutto.GetValue();


        }

        //decimal getOpcjeKatalogoweNierabatowane()
        //{
        //    return 0;
        //}





    }
}
