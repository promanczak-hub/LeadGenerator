using Express.Logic.Kalkulatory.LTR.CalculatorReport;
using Express.WD;
using System;
using System.Linq;
using TF.BusinessFramework.Patterns.UnitOfWork;

namespace Express.Logic.Kalkulatory.LTR
{
    public class LTRSubCalculatorSamochodZastepczy : LTRSubCalculator
    {

        public class Data : LTRSubCalculatorData
        {

        }

        public class Result : LTRSubCalculatorResult
        {
            public decimal StawkaZaZastepczyNetto { get; set; }
        }

        public LTRSubCalculatorSamochodZastepczy(Kalkulacja kalkulacja, LTRTabele tabele)
            : base(kalkulacja, tabele)
        {

        }

        public Result Policz(Data data)
        {

            decimal stawka = 0;

            if (_kalkulacja.SamochodZastepczy.IsTrue())
            {

                int? klasaId = _kalkulacja.Model.KlasaId;

                if (klasaId == null)
                    throw new Exception("Nie określono klasy na modelu");

                
                LTRAdminStawkaZastepczy pozycja = _tabele.StawkaZastepczy
                  .Where(p => p.KlasaId == klasaId)
                  .FirstOrDefault();

                //if (null == pozycja)
                //{
                //    string symbol = _kalkulacja.Model.SymbolEuroTax.Symbol;
                //    if (symbol != null && symbol.Length > 2)
                //    {
                //        string symbolEurotaxBase = _kalkulacja.Model.SymbolEuroTax.Symbol.Substring(0, 2);
                //        pozycja = _tabele.StawkaZastepczy.FirstOrDefault(w => w.SymbolEuroTax.Symbol == symbolEurotaxBase);
                //    }
                //}

                if (null == pozycja)
                    throw new Exception("Brak pozycji w tabeli LTRAdminStawkaZastepczy");
                if (null == pozycja.SredniaIloscDobWRoku)
                    throw new Exception("Brak parametru SredniaIloscDobWRoku w pozycji LTRAdminStawkaZastepczy");
                if (null == pozycja.DobaNetto)
                    throw new Exception("Brak parametru DobaNetto w pozycji LTRAdminStawkaZastepczy");

                decimal okresUzytkowania = (decimal)_tabele.Okres;
                stawka = pozycja.SredniaIloscDobWRoku.GetValue() * pozycja.DobaNetto.Wartosc * okresUzytkowania / 12m;

                if (_tabele.IsReport)
                {
                    CalcReportNameValueTable table = new CalcReportNameValueTable("SAMOCHÓD ZASTĘPCZY");
                    _tabele.Report.AddItem(table);
                    table.AddValue("klasaId", pozycja.KlasaId);
                    if (null != pozycja.Klasa)
                        table.AddValue("klasa", pozycja.Klasa.Nazwa);
                    table.AddValue("sredniaIloscDobWRoku", pozycja.SredniaIloscDobWRoku.GetValue());
                    table.AddValue("dobaNetto", Kwota.GetAsDecimal(pozycja.DobaNetto));
                    table.AddValue("okresUzytkowania", okresUzytkowania);
                    table.AddValue("stawkaZaZastepczy", stawka, true);
                }
            }
            else
            {
                if (_tabele.IsReport)
                {
                    CalcReportNameValueTable table = new CalcReportNameValueTable("SAMOCHÓD ZASTĘPCZY");
                    _tabele.Report.AddItem(table);
                    table.AddValue("stawkaZaZastepczy", stawka, true);
                }
            }

            return new Result()
            {
                StawkaZaZastepczyNetto = stawka
            };

        }







    }
}
