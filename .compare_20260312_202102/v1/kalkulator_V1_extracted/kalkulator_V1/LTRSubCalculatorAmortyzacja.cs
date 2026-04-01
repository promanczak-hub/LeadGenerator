using Express.Logic.Kalkulatory.LTR.CalculatorReport;
using Express.WD;

namespace Express.Logic.Kalkulatory.LTR
{
    public class LTRSubCalculatorAmortyzacja : LTRSubCalculator
    {

        public class Data : LTRSubCalculatorData
        {
            public decimal WP { get; set; }
            public decimal WR { get; set; }
        }

        public class Result : LTRSubCalculatorResult
        {
            public decimal AmortyzacjaProcent { get; set; }
        }

        public LTRSubCalculatorAmortyzacja(Kalkulacja kalkulacja, LTRTabele tabele)
            : base(kalkulacja, tabele)
        {

        }

        public  Result Policz(Data data)
        {

            Data d = data;


            decimal utrataWartosci = d.WP - d.WR;
            decimal kwotaAmortyzacji1Miesiac = utrataWartosci / _tabele.Okres;
            decimal procentAmortyzacji = kwotaAmortyzacji1Miesiac / d.WP;


            if (_tabele.IsReport)
            {
                CalcReportNameValueTable table = new CalcReportNameValueTable("AMORTYZACJA");
                _tabele.Report.AddItem(table);
                table.AddValue("WP", d.WP);
                table.AddValue("WR", d.WR);
                table.AddValue("utrataWartosci", utrataWartosci);
                table.AddValue("kwotaAmortyzacji1Miesiac", kwotaAmortyzacji1Miesiac);
                table.AddValue("procentAmortyzacji", procentAmortyzacji);
            }

            return new Result() { AmortyzacjaProcent = procentAmortyzacji };
        }

    }
}
