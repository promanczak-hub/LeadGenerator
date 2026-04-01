using Express.Logic.Kalkulatory.LTR.CalculatorReport;
using Express.WD;
using System.Collections.Generic;

namespace Express.Logic.Kalkulatory.LTR
{
    public class LTRSubCalculatorKosztDzienny : LTRSubCalculator
    {

        const decimal COEFF_KOSZT_DZIENNY = 30.4m;

        public class Data : LTRSubCalculatorData
        {
            public decimal UtrataWartosciZczynszem { get; set; }
            public decimal UtrataWartosciBEZczynszu { get; set; }
            public decimal KosztFinansowy { get; set; }

            public decimal SamochodZastepczyNetto { get; set; }
            public decimal KosztyDodatkoweNetto { get; set; }
            public decimal UbezpieczenieNetto { get; set; }
            public decimal OponyNetto { get; set; }
            public decimal SerwisNetto { get; set; }

            public decimal SumaOdsetekBezCzynszuInicjalnego { get; set; }

        }

        public class Result : LTRSubCalculatorResult
        {
            public decimal KosztDzienny { get; set; }
            public decimal KosztyOgolem { get; set; }
            public decimal KosztMC { get; set; }
            public decimal KosztMcBEZcz { get; set; }
        }

        public LTRSubCalculatorKosztDzienny(Kalkulacja kalkulacja, LTRTabele tabele)
            : base(kalkulacja, tabele)
        {

        }


        public Result Policz(Data data)
        {
            

            decimal lacznyKosztFinansowy = data.KosztFinansowy + data.UtrataWartosciZczynszem;

            decimal lacznyKosztTechniczny = data.SamochodZastepczyNetto + data.KosztyDodatkoweNetto +
                data.UbezpieczenieNetto + data.OponyNetto + data.SerwisNetto;

            decimal kosztyOgolem = lacznyKosztFinansowy + lacznyKosztTechniczny;

            decimal kosztyMiesiac = kosztyOgolem / _tabele.Okres;

            decimal kosztDzienny = kosztyMiesiac / COEFF_KOSZT_DZIENNY;


            decimal SYM_lacznyKosztFinansowy = data.UtrataWartosciBEZczynszu + data.SumaOdsetekBezCzynszuInicjalnego;
            decimal SYM_lacznyKosztTechniczny = lacznyKosztTechniczny;
            decimal SYM_kosztyOgolem = SYM_lacznyKosztFinansowy + SYM_lacznyKosztTechniczny;
            decimal SYM_kosztyMiesiac = SYM_kosztyOgolem / _tabele.Okres;

            if (_tabele.IsReport)
            {

                List<string> headers = new List<string>() { "NETTO", "KOSZTY SYMULOWANE (BEZ UWZGLDĘDNIENIA CZ.INICJALNEGO)" };
                CalcReportTable table = new CalcReportTable("KOSZT DZIENNY", headers);
                _tabele.Report.AddItem(table);
                table.AddRow("lacznyKosztFinansowy", new List<decimal>() { lacznyKosztFinansowy, SYM_lacznyKosztFinansowy });
                table.AddRow("lacznyKosztTechniczny", new List<decimal>() { lacznyKosztTechniczny, SYM_lacznyKosztTechniczny });
                table.AddRow("kosztyOgolem", new List<decimal>() { kosztyOgolem, SYM_kosztyOgolem });
                table.AddRow("kosztyMiesiac", new List<decimal>() { kosztyMiesiac, SYM_kosztyMiesiac });
                table.AddRow("kosztDzienny", new List<decimal>() { kosztDzienny, 0 });

            }

            return new Result
            {
                KosztDzienny = kosztDzienny,
                KosztMC = kosztyMiesiac,
                KosztMcBEZcz = SYM_kosztyMiesiac,
                KosztyOgolem = kosztyOgolem
            };

        }


    }
}
