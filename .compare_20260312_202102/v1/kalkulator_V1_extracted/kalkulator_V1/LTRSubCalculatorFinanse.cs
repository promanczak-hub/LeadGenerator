using Express.Logic.Kalkulatory.LTR.CalculatorReport;
using Express.WD;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Express.Logic.Kalkulatory.LTR
{
    public class LTRSubCalculatorFinanse : LTRSubCalculator
    {
        public class Data : LTRSubCalculatorData
        {
            public decimal WartoscPoczatkowaNetto { get; set; }
            public decimal WrPrzewidywanaCenaSprzedazy { get; set; }
        }

        public class Result : LTRSubCalculatorResult
        {
            public decimal SumaOdsetekZczynszem { get; set; }
            public decimal SumaOdsetekBEZczynszu { get; set; }
            public decimal CzynszInicjalnyProcent { get; set; }
            public decimal CzynszInicjalnyNetto { get; set; }
            public decimal MarzaFinansowaProcent { get; set; }
        }

        class Rata
        {
            public int NumerRaty { get; set; }
            public decimal KapitalDoSplaty { get; set; }
            public decimal RataLeasingowa { get; set; }
            public decimal RataKapitalowa { get; set; }
            public decimal KapitalPoSplacie { get; set; }
            public decimal RataOdsetkowa { get; set; }

        }

        class RataCalculationResult
        {
            public decimal RataProcent { get; set; }
            public decimal RataKwota { get; set; }

            public List<Rata> Raty { get; set; }

            public decimal SumaOdsetek { get; set; }
        }

        public LTRSubCalculatorFinanse(Kalkulacja kalkulacja, LTRTabele tabele) : base(kalkulacja, tabele)
        {

        }

        public Result Policz(Data data)
        {
           

            if (data.WartoscPoczatkowaNetto == 0)
                throw new Exception("Parametr WartoscPoczatkowaNetto nie może być równy 0");
            if (null == _kalkulacja.CzynszInicjalny)
                throw new Exception("Brak parametru CzynszInicjalny w Kalkulacji");

            decimal wartoscKredytu = data.WartoscPoczatkowaNetto - (Kwota.GetAsDecimal(_kalkulacja.CzynszInicjalny) / _tabele.Parametry.StawkaVAT);
            decimal czynszProcent = _kalkulacja.CzynszProcent;
            decimal wykupKwota = Math.Min(wartoscKredytu, data.WrPrzewidywanaCenaSprzedazy);
            decimal wykupProcent = wykupKwota / data.WartoscPoczatkowaNetto;
            decimal wibor = _kalkulacja.WIBORProcent.GetValue();
            decimal marzaFinansowa = _kalkulacja.MarzaFinansowaProcent.GetValue();
            decimal oprocentowanie = wibor + marzaFinansowa;

            int iloscRat = _tabele.Okres;

            RataCalculationResult zCzynszemResult = getRaty(wartoscKredytu, wykupKwota, iloscRat, oprocentowanie);

            RataCalculationResult noRentResult = getRaty(data.WartoscPoczatkowaNetto, wykupKwota, iloscRat, oprocentowanie);

            if (_tabele.IsReport)
            {
                CalcReportNameValueTable table = new CalcReportNameValueTable("FINANSOWE");
                _tabele.Report.AddItem(table);
                table.AddValue("WartoscPoczatkowaNetto", data.WartoscPoczatkowaNetto);
                table.AddValue("wartoscKredytu", wartoscKredytu);
                table.AddValue("czynszInicjalnyProcent", czynszProcent);
                table.AddValue("wykupProcent", wykupProcent);
                table.AddValue("wykupKwota", wykupKwota);
                table.AddValue("iloscRat", iloscRat);
                table.AddValue("wibor", wibor);
                table.AddValue("marzaFinansowa", marzaFinansowa);
                table.AddValue("oprocentowanie", oprocentowanie);

                addTablesToResult(_tabele.Report, zCzynszemResult, "z czynszem inicjalnym (min WR/WK)");
                addTablesToResult(_tabele.Report, noRentResult, "bez czynszu inicjalnego");
            }

            return new Result
            {
                SumaOdsetekZczynszem = zCzynszemResult.SumaOdsetek,
                SumaOdsetekBEZczynszu = noRentResult.SumaOdsetek,
                CzynszInicjalnyProcent = czynszProcent,
                MarzaFinansowaProcent = marzaFinansowa,
                CzynszInicjalnyNetto = Kwota.GetAsDecimal(_kalkulacja.CzynszInicjalny) / _tabele.Parametry.StawkaVAT
            };

        }

        void addTablesToResult(CalcReport report, RataCalculationResult rataCalcResult, string comment)
        {
            CalcReportNameValueTable tableRataValue = new CalcReportNameValueTable(string.Empty);
            report.AddItem(tableRataValue);
            tableRataValue.AddValue("rataProcent", rataCalcResult.RataProcent);
            tableRataValue.AddValue("rataKwota", rataCalcResult.RataKwota);

            List<string> tableRowHeaders = rataCalcResult.Raty.Select(r => r.NumerRaty.ToString()).ToList();
            CalcReportTable tableAllInstalments = new CalcReportTable(string.Empty, tableRowHeaders);
            report.AddItem(tableAllInstalments);
            tableAllInstalments.AddRow("Kapitał do spłaty", rataCalcResult.Raty.Select(r => r.KapitalDoSplaty).ToList());
            tableAllInstalments.AddRow("Rata leasingowa", rataCalcResult.Raty.Select(r => r.RataLeasingowa).ToList());
            tableAllInstalments.AddRow("Rata kapitalowa", rataCalcResult.Raty.Select(r => r.RataKapitalowa).ToList());
            tableAllInstalments.AddRow("Kapitał po spłacie", rataCalcResult.Raty.Select(r => r.KapitalPoSplacie).ToList());
            tableAllInstalments.AddRow("Rata odsetkowa", rataCalcResult.Raty.Select(r => r.RataOdsetkowa).ToList());

            CalcReportNameValueTable tableSum = new CalcReportNameValueTable(string.Empty);
            report.AddItem(tableSum);
            tableSum.AddValue(string.Format("Suma odsetek {0}", comment), rataCalcResult.SumaOdsetek);


        }

        private decimal GetRataProcent(decimal wartoscKredytu, decimal wykup, int liczbaRat, decimal oprocentowanie)
        {
            if (wartoscKredytu == 0)
                throw new Exception("Parametr WartoscKredytu nie może być równy 0");

            PMT.Result result = PMT.GetResult(wartoscKredytu, wykup, oprocentowanie, 1, liczbaRat);
            decimal rataProcent = result.OplataLeasingowa / wartoscKredytu;
            return rataProcent;

        }

        RataCalculationResult getRaty(decimal wartoscKredytu, decimal wykupKwota, int iloscRat, decimal oprocentowanie)
        {
            decimal rataProcent = GetRataProcent(wartoscKredytu, wykupKwota, iloscRat, oprocentowanie);
            decimal rataKwota = wartoscKredytu * rataProcent;

            List<Rata> raty = new List<Rata>();

            for (int r = 1; r <= iloscRat; r++)
            {
                Rata rata = new Rata();
                rata.NumerRaty = r;

                Rata poprzednia;
                if (r == 1)
                    poprzednia = new Rata();
                else
                    poprzednia = raty.Last();

                if (r == 1)
                    rata.KapitalDoSplaty = wartoscKredytu;
                else
                    rata.KapitalDoSplaty = poprzednia.KapitalPoSplacie;


                if (rata.NumerRaty > 0 && rata.NumerRaty < iloscRat + 1)
                    rata.RataLeasingowa = rataKwota;
                else if (rata.NumerRaty == iloscRat + 1)
                    rata.RataLeasingowa = wykupKwota;

                if (rata.NumerRaty < rata.NumerRaty + 1)
                    rata.RataOdsetkowa = rata.KapitalDoSplaty * oprocentowanie / 12m;

                rata.RataKapitalowa = rata.RataLeasingowa - rata.RataOdsetkowa;

                rata.KapitalPoSplacie = rata.KapitalDoSplaty - rata.RataKapitalowa;

                raty.Add(rata);
            }

            decimal sumaOdsetek = raty.Sum(r => r.RataOdsetkowa);


            RataCalculationResult result = new RataCalculationResult()
            {
                RataKwota = rataKwota,
                RataProcent = rataProcent,
                Raty = raty,
                SumaOdsetek = sumaOdsetek
            };

            return result;


        }
    }
}
