using Express.Logic.Kalkulatory.LTR.CalculatorReport;
using Express.WD;

namespace Express.Logic.Kalkulatory.LTR
{
    public class LTRSubCalculatorBudzetMarketingowy : LTRSubCalculator
    {
        public class Data : LTRSubCalculatorData
        {
            public decimal UtrataWartosciZCzynszemInicjalnym { get; set; }
            public decimal WRPrzewidywanaCenaSprzedazy { get; set; }
        }

        public class Result : LTRSubCalculatorResult
        {
            public decimal KorektaWRMaks { get; set; }
        }

        public LTRSubCalculatorBudzetMarketingowy(Kalkulacja kalkulacja, LTRTabele tabele) : base(kalkulacja, tabele)
        {
        }


        public Result Policz(Data data)
        {
           

            decimal korektaWRMaksBrutto = data.WRPrzewidywanaCenaSprzedazy * _tabele.Parametry.StawkaVAT * _tabele.Parametry.BudzetMarketingowyLtr;

            if (_tabele.IsReport)
            {


                CalcReportNameValueTable table = new CalcReportNameValueTable("BUDŻET MARKETINGOWY");
                _tabele.Report.AddItem(table);
                table.AddValue("korektaWRMaksBrutto", korektaWRMaksBrutto);
            }

            return new Result()
            {
                KorektaWRMaks = korektaWRMaksBrutto
            };
        }
    }
}
