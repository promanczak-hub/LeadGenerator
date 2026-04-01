using Express.Logic.Kalkulatory.LTR.CalculatorReport;
using Express.WD;
using System;
using System.Collections.Generic;
using System.Linq;
using TF.BusinessFramework.Patterns.UnitOfWork;

namespace Express.Logic.Kalkulatory.LTR
{
    public class LTRSubCalculatorUbezpieczenie : LTRSubCalculator
    {
        const int LICZBA_LAT = 7;

        public class Data : LTRSubCalculatorData
        {
            public decimal AmortyzacjaProcent { get; set; }
            public decimal CenaZakupu { get; set; }
        }

        public class Result : LTRSubCalculatorResult
        {
            public decimal SkladkaWCalymOkresieNetto { get; set; }
        }

        public LTRSubCalculatorUbezpieczenie(Kalkulacja kalkulacja, LTRTabele tabele) : base(kalkulacja, tabele)
        {

        }

        class Pozycja
        {
            public int Rok { get; set; }
            public decimal PostawaNaliczania { get; set; }
            public decimal SkladkaAC { get; set; }
            public decimal SkladkaBazowaAC { get; set; }
            public decimal SkladkaOC { get; set; }
            public decimal DoubezpieczenieKradziezy { get; set; }
            public decimal DoubezpieczenieNaukaJazdu { get; set; }
            public decimal SkladkaRoczna { get; set; }
            public decimal SredniaWartoscSzkodyRocznie { get; set; }
            public decimal SkladkaLacznie { get; set; }
            public decimal SkladkaWCalymOkresie { get; set; }
        }

        public  Result Policz(Data data)
        {
            

            CalcReportNameValueTable sredniaWartoscSzkodyReport;

            decimal sredniaWartoscSzkody = getSredniaWartoscSzkody(data, out sredniaWartoscSzkodyReport);
            decimal amortyzacja = data.AmortyzacjaProcent;
            int okresUzytkowania = _tabele.Okres;

            decimal doubezpieczenieKradziezyProcent = (_kalkulacja.DoubezpieczenieKradziezy.IsTrue()) ?
                _tabele.Parametry.DoubezpieczenieKradziezy : 0;

            decimal doubezpieczenieNaukaJazdyProcent = (_kalkulacja.NaukaJazdy.IsTrue()) ?
                _tabele.Parametry.NaukaJazdy : 0;

            int? klasaId = _kalkulacja.Model != null ? _kalkulacja.Model.KlasaId : null;

            List<Pozycja> pozycje = new List<Pozycja>();
            for (int r = 1; r <= LICZBA_LAT; r++)
            {                
                LTRAdminUbezpieczenie pozycjaWTabeli = null;
                if (klasaId.HasValue)
                    pozycjaWTabeli = _tabele.Ubezpieczenie.Where(p => p.KolejnyRok == r && p.KlasaId == klasaId.Value).FirstOrDefault();
                if (pozycjaWTabeli == null)
                    pozycjaWTabeli = _tabele.Ubezpieczenie.Where(p => p.KolejnyRok == r && p.KlasaId == null).FirstOrDefault();
                
                if (null == pozycjaWTabeli)
                    throw new Exception("Brak pozycji w tabeli LTRAdminUbezpieczenie");

                Pozycja pozycja = new Pozycja();
                pozycja.Rok = r;

                int liczbaMiesiecy = (r - 1) * 12;
                pozycja.PostawaNaliczania = data.CenaZakupu * (1 - liczbaMiesiecy * amortyzacja);

                pozycja.SkladkaBazowaAC = pozycjaWTabeli.StawkaBazowaAC.Value;
                pozycja.SkladkaAC = Math.Round(pozycjaWTabeli.StawkaBazowaAC.Value * pozycja.PostawaNaliczania);
                pozycja.SkladkaOC = Kwota.GetAsDecimal(pozycjaWTabeli.SkladkaOC);

                pozycja.DoubezpieczenieKradziezy = pozycja.SkladkaAC * doubezpieczenieKradziezyProcent;
                pozycja.DoubezpieczenieNaukaJazdu = pozycja.SkladkaAC * doubezpieczenieNaukaJazdyProcent;


                pozycja.SkladkaRoczna = getSkladkaRoczna(pozycja);

                pozycja.SredniaWartoscSzkodyRocznie = getSredniaWartoscSzkodyRok(sredniaWartoscSzkody, okresUzytkowania, r);

                pozycja.SkladkaLacznie = pozycja.SkladkaRoczna + pozycja.SredniaWartoscSzkodyRocznie;

                if (_kalkulacja.ExpressPlaciUbezpieczenie.IsTrue())
                {
                    int okresMin = (r - 1) * 12;
                    int okresMax = r * 12;

                    if (okresUzytkowania > okresMin &&
                        okresUzytkowania <= okresMax)
                    {
                        pozycja.SkladkaWCalymOkresie = pozycje.Sum(p => p.SkladkaLacznie) + pozycja.SkladkaLacznie;
                    }
                }
                pozycje.Add(pozycja);
            }

            decimal skladkaLacznieSuma = 0;
            if (_kalkulacja.ExpressPlaciUbezpieczenie.IsTrue())
            {
                skladkaLacznieSuma = pozycje.Sum(p => p.SkladkaLacznie);

                if (_kalkulacja.KorektaKosztuUbezpieczenia.IsTrue())
                    skladkaLacznieSuma += (Kwota.GetAsDecimal(_kalkulacja.KosztUbezpieczeniaKorekta) / _tabele.Parametry.StawkaVAT);
            }

            Result result = new Result()
            {
                SkladkaWCalymOkresieNetto = skladkaLacznieSuma
            };

            if (_tabele.IsReport)
            {
                List<string> tableHeaders = pozycje.Select(p => p.Rok.ToString()).ToList();
                CalcReportTable table = new CalcReportTable("UBEZPIECZENIE", tableHeaders);
                table.AddRow("PostawaNaliczania", pozycje.Select(p => p.PostawaNaliczania).ToList());
                table.AddRow("SkladkaBazowaAC", pozycje.Select(p => p.SkladkaBazowaAC).ToList());
                table.AddRow("SkladkaAC", pozycje.Select(p => p.SkladkaAC).ToList());
                table.AddRow("SkladkaOC", pozycje.Select(p => p.SkladkaOC).ToList());
                table.AddRow("DoubezpieczenieKradziezy", pozycje.Select(p => p.DoubezpieczenieKradziezy).ToList());
                table.AddRow("DoubezpieczenieNaukaJazdu", pozycje.Select(p => p.DoubezpieczenieNaukaJazdu).ToList());
                table.AddRow("SkladkaRoczna", pozycje.Select(p => p.SkladkaRoczna).ToList());
                table.AddRow("SredniaWartoscSzkodyRocznie", pozycje.Select(p => p.SredniaWartoscSzkodyRocznie).ToList());
                table.AddRow("SkladkaLacznie", pozycje.Select(p => p.SkladkaLacznie).ToList());
                table.AddRow("SkladkaWCalymOkresie", pozycje.Select(p => p.SkladkaWCalymOkresie).ToList());
                _tabele.Report.AddItem(table);

                if (null != sredniaWartoscSzkodyReport)
                    _tabele.Report.AddItem(sredniaWartoscSzkodyReport);
            }

            return result;



        }

        #region helper methods
        decimal getSkladkaRoczna(Pozycja pozycja)
        {
            decimal suma = pozycja.SkladkaAC + pozycja.SkladkaOC + pozycja.DoubezpieczenieKradziezy + pozycja.DoubezpieczenieNaukaJazdu;

            int liczbaMiesiecy = _tabele.Okres;
            int miesiace = pozycja.Rok * 12;
            int miesiacePoprzedni = (pozycja.Rok - 1) * 12;

            if (pozycja.Rok == 1)
            {
                if (_kalkulacja.OkresUzytkowania >= 12)
                    return suma;

                decimal result = (decimal)_kalkulacja.OkresUzytkowania / 12.0M;
                result *= suma;
                return result;
            }
            else
            {
                if (liczbaMiesiecy < miesiace && liczbaMiesiecy > miesiacePoprzedni)
                {
                    decimal coeff = (((decimal)(liczbaMiesiecy - miesiacePoprzedni)) / 12m);
                    decimal skladka = coeff * suma;
                    return skladka;
                }
                else
                {
                    if (liczbaMiesiecy >= miesiace)
                        return suma;
                    else
                        return 0;
                }
            }

        }

        decimal getSredniaWartoscSzkody(Data data, out CalcReportNameValueTable reportTable)
        {
            int? klasaId = _kalkulacja.Model.KlasaId;

            if (klasaId == null)
                throw new Exception("Nie określono klasy na modelu");

            if (_tabele.Parametry.SredniPrzebiegDlaSzkody == 0)
                throw new Exception("Parametr 'Sredni przebieg dla szkody' ma wartosc 0");

            decimal sredniaWartoscSzkody = Kwota.GetAsDecimal(_tabele.Parametry.SredniaWartoscSzkody);
            decimal sredniPrzebiegDlaSzkody = (decimal)_tabele.Parametry.SredniPrzebiegDlaSzkody;
            decimal przebiegKoncowy = (decimal)_tabele.Przebieg;

            LTRAdminWspolczynnikiSzkodowe pozycja = _tabele.WspolczynnikiSzkodowe
                .Where(w => w.KlasaId == klasaId.Value)
                .FirstOrDefault();

            //if (null == pozycja)
            //{
            //    string symbol = _kalkulacja.Model.SymbolEuroTax.Symbol;
            //    if (symbol != null && symbol.Length > 2)
            //    {
            //        string symbolEurotaxBase = _kalkulacja.Model.SymbolEuroTax.Symbol.Substring(0, 2);
            //        pozycja = _tabele.WspolczynnikiSzkodowe.FirstOrDefault(w => w.SymbolEuroTax.Symbol == symbolEurotaxBase);
            //    }
            //}

            if (null == pozycja)
                throw new Exception("Brak pozycji w tabeli LTRAdminWspolczynnikiSzkodowe");


            decimal wspSredniPrzebieg = pozycja.WspSredniPrzebieg.Value;
            decimal wspWartoscSzkody = pozycja.WspWartoscSzkody.Value;

            if (_tabele.IsReport)
            {
                reportTable = new CalcReportNameValueTable("sredniaWartoscSzkody");
                reportTable.AddValue("klasaId", pozycja.KlasaId);
                if (null != pozycja.Klasa)
                    reportTable.AddValue("klasa", pozycja.Klasa.Nazwa);
                reportTable.AddValue("wspSredniPrzebieg", pozycja.WspSredniPrzebieg);
                reportTable.AddValue("WspWartoscSzkody", pozycja.WspWartoscSzkody);
            }
            else
                reportTable = null;

            decimal srednia =
                sredniaWartoscSzkody * (przebiegKoncowy / sredniPrzebiegDlaSzkody * wspSredniPrzebieg * wspWartoscSzkody);

            return srednia;

        }

        decimal getSredniaWartoscSzkodyRok(decimal sredniaWartoscSzkody, int okresUzytkowania, int rok)
        {


            int v1 = rok * 12;
            int v2 = (rok - 1) * 12;


            if (okresUzytkowania <= v1 && okresUzytkowania > v2)
            {

                return sredniaWartoscSzkody / okresUzytkowania * (okresUzytkowania - v2);

            }
            else
            {
                if (okresUzytkowania > v1)
                {
                    return sredniaWartoscSzkody / okresUzytkowania * 12;
                }
                else
                {
                    return 0;
                }
            }

        }
        #endregion
    }
}
