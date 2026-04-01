using Express.Logic.Kalkulatory.LTR.CalculatorReport;
using Express.Logic.Kalkulatory.LTR.CalculatorTypes;
using Express.Types.Kalkulatory;
using Express.Types.Magazyny;
using Express.WD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Express.Logic.Kalkulatory.LTR
{
    public class LTRSubCalculatorUtrataWartosci : LTRSubCalculator
    {
        public class Data : LTRSubCalculatorData
        {
            public decimal CenaZakupuBezOponOpcjiSerwisowychIpakietuSerwisowego { get; set; }
            public Dictionary<int, decimal> TabelaEurotax { get; internal set; }
        }

        public LTRSubCalculatorUtrataWartosci(Kalkulacja kalkulacja, LTRTabele tabele) : base(kalkulacja, tabele)
        {

        }

        public LTRSubCalculatorUtrataWartosciNewOutput Policz(Data data)
        {
            if (null == _kalkulacja.CenaCennikowa)
                throw new Exception("Brak parametru Cena Cennikowa w kalkulacji");

            CalcReportNameValueTable cenaUzywanegoMinus1report = null;
            CalcReportNameValueTable cenaUzywanegoMinus1reportInitial = null;
            CalcReportNameValueTable przebiegNormatywnyReport = null;
            CalcReportNameValueTable korektaZaPrzebiegReport = null;
            CalcReportNameValueTable korektaAdministracyjnaReport = null;


            int miesiacStart = 0;
            int przebiegStart = 0;
            if (data.StarySamochod != null && data.StarySamochod.UwzglednijParametryStaregowWR)
            {
                miesiacStart = data.StarySamochod.WiekPoczatkowy;
                przebiegStart = data.StarySamochod.PrzebiegPoczatkowy;
            }

            decimal cenaCennikowaNetto = Kwota.GetAsDecimal(_kalkulacja.CenaCennikowa) / _tabele.Parametry.StawkaVAT;
            decimal cenaUzywanegoBezKorekty = 0;
            bool czyBiezacyRocznik = _kalkulacja.Rocznik == LTRRocznikConst.Biezacy;
            if (czyBiezacyRocznik)
                cenaUzywanegoBezKorekty = GetCenaUzywanego(_tabele.Okres + miesiacStart, data.TabelaEurotax) / GetCenaUzywanego(miesiacStart, data.TabelaEurotax) * cenaCennikowaNetto;
            else
                cenaUzywanegoBezKorekty = GetCenaUzywanegoMinus1(_tabele.Okres + miesiacStart, out cenaUzywanegoMinus1report, data.TabelaEurotax) / GetCenaUzywanegoMinus1(miesiacStart, out cenaUzywanegoMinus1reportInitial, data.TabelaEurotax) * cenaCennikowaNetto;

            decimal doposazenieFabryczneBrutto = _kalkulacja.OpcjeFabryczne.Sum(s => Kwota.GetAsDecimal(s.CenaCennikowa));
            decimal doposazenieFabryczneNetto = doposazenieFabryczneBrutto / _tabele.Parametry.StawkaVAT;
            decimal doposazenieSerwisoweWRBrutto = _kalkulacja.OpcjaSerwisowas.Where(a => a.WR).Sum(s => Kwota.GetAsDecimal(s.CenaCennikowa));
            decimal dopozazenieSeriwsoweWRNetto = doposazenieSerwisoweWRBrutto / _tabele.Parametry.StawkaVAT;
            decimal WRdlaWyposazenia = (doposazenieFabryczneNetto + dopozazenieSeriwsoweWRNetto) /
                                       (1.0m + ((decimal)(_tabele.Okres) / 12.0m)) * (1.0m + ((decimal)miesiacStart) / 12.0m);


            decimal przebiegNormatywnyZa1Miesiac = GetPrzebiegNormatywny(miesiacStart, out przebiegNormatywnyReport);
            decimal przebiegNormatywnyLacznie = przebiegNormatywnyZa1Miesiac * (_tabele.Okres + miesiacStart);
            decimal nadprzebiegNiedobieg = przebiegNormatywnyLacznie - ( _tabele.Przebieg + przebiegStart);
            decimal korektaZaPrzebiegPLN = GetKorektaZaPrzebieg(cenaUzywanegoBezKorekty, nadprzebiegNiedobieg, out korektaZaPrzebiegReport);
            decimal WRpoKorekcieZaPrzebieg = cenaUzywanegoBezKorekty + WRdlaWyposazenia + korektaZaPrzebiegPLN;
            decimal korektaAdministracyjnaProcent = GetKorektaAdministracyjna(WRpoKorekcieZaPrzebieg, out korektaAdministracyjnaReport);
            decimal korektaAdministracyjnaKwotowo = WRpoKorekcieZaPrzebieg * korektaAdministracyjnaProcent;

            // co robić w przypadku, gdzy klasa byłaby pusta?
            string klasa = _kalkulacja.Model != null ? _kalkulacja.Model.Klasa != null ? _kalkulacja.Model.Klasa.Nazwa : string.Empty : string.Empty;
            string klasaExpressPaliwo = klasa + " - " + _kalkulacja.RodzajPaliwa;

            decimal korektaRVdlaLTR = GetKorektaRVDlaLTR();
            decimal wrPrzewidywanaCenaSprzedazy = GetWRPrzewidywanaCenaSprzedazy(WRpoKorekcieZaPrzebieg, korektaAdministracyjnaProcent, korektaRVdlaLTR, _kalkulacja.KorektaWR);
            decimal wrPrzewidywanaCenaSprzedazyDlaLO = wrPrzewidywanaCenaSprzedazy * (1 + _tabele.Parametry.PrzewidywanaCenaSprzedazyLO);

            // UTRATA WARTOSCI
            decimal sumaOpcjiBrutto = _kalkulacja.OpcjaSerwisowas.Sum(s => Kwota.GetAsDecimal(s.CenaCennikowa));
            decimal sumaOpcjiNetto = sumaOpcjiBrutto / _tabele.Parametry.StawkaVAT;

            decimal utrataWartosciBEZczynszu = 0;
            if (data.CenaZakupuBezOponOpcjiSerwisowychIpakietuSerwisowego + sumaOpcjiNetto - wrPrzewidywanaCenaSprzedazy < 0)
                utrataWartosciBEZczynszu = 0;
            else
                utrataWartosciBEZczynszu = data.CenaZakupuBezOponOpcjiSerwisowychIpakietuSerwisowego + sumaOpcjiNetto - wrPrzewidywanaCenaSprzedazy;


            // UTRATA WARTOSCI z czynszem inicjalnym
            decimal czynszInicjalnyNetto = Kwota.GetAsDecimal(_kalkulacja.CzynszInicjalny) / _tabele.Parametry.StawkaVAT;
            decimal utrataWartosciZczynszemInicjalnym = 0;
            if (utrataWartosciBEZczynszu < czynszInicjalnyNetto)
                utrataWartosciZczynszemInicjalnym = 0;
            else
                utrataWartosciZczynszemInicjalnym = utrataWartosciBEZczynszu - czynszInicjalnyNetto;


            if (_tabele.IsReport)
            {

                CalcReportNameValueTable table = new CalcReportNameValueTable("UTRATA WARTOŚCI");
                _tabele.Report.AddItem(table);

                table.AddValue("cenaCennikowaNetto", cenaCennikowaNetto);
                table.AddValue("cenaUzywanegoBezKorekty", cenaUzywanegoBezKorekty);
                table.AddValue("doposazenieFabryczneNetto", doposazenieFabryczneNetto);
                table.AddValue("dopozazenieSeriwsoweNetto", dopozazenieSeriwsoweWRNetto);
                table.AddValue("WRdlaWyposazenia", WRdlaWyposazenia);
                table.AddValue("przebiegNormatywnyZa1Miesiac", przebiegNormatywnyZa1Miesiac);
                table.AddValue("przebiegNormatywnyLacznie", przebiegNormatywnyLacznie);
                table.AddValue("nadprzebiegNiedobieg", nadprzebiegNiedobieg);
                table.AddValue("korektaZaPrzebiegPLN", korektaZaPrzebiegPLN);
                table.AddValue("WRpoKorekcieZaPrzebieg", WRpoKorekcieZaPrzebieg);
                table.AddValue("korektaAdministracyjnaProcent", korektaAdministracyjnaProcent);
                table.AddValue("korektaAdministracyjnaKwotowo", korektaAdministracyjnaKwotowo);
                table.AddValue("WRPrzewidywanaCenaSprzedazy", wrPrzewidywanaCenaSprzedazy);
                table.AddValue("WRprzewidywanaCenaSprzedazyDlaLO", wrPrzewidywanaCenaSprzedazyDlaLO);
                table.AddValue("utrataWartosci", utrataWartosciBEZczynszu, true);
                table.AddValue("utrataWartosciZczynszemInicjalnym", utrataWartosciZczynszemInicjalnym);
                table.AddValue("klasaExpressPaliwo", klasaExpressPaliwo);

                if (null != cenaUzywanegoMinus1report)
                    _tabele.Report.AddItem(cenaUzywanegoMinus1report);

                if (null != przebiegNormatywnyReport)
                    _tabele.Report.AddItem(przebiegNormatywnyReport);

                if (null != korektaZaPrzebiegReport)
                    _tabele.Report.AddItem(korektaZaPrzebiegReport);

                if (null != korektaAdministracyjnaReport)
                    _tabele.Report.AddItem(korektaAdministracyjnaReport);
            }

            return new LTRSubCalculatorUtrataWartosciNewOutput
            {
                UtrataWartosciBEZczynszu = utrataWartosciBEZczynszu,
                UtrataWartosciZCzynszemInicjalnym = utrataWartosciZczynszemInicjalnym,
                KorektaZaPrzebiegKwotowo = korektaZaPrzebiegPLN,
                KorektaAdministracyjnaKwotowo = korektaAdministracyjnaKwotowo,
                WR = wrPrzewidywanaCenaSprzedazy,
                WRdlaLO = wrPrzewidywanaCenaSprzedazyDlaLO,
            };

        }

        private decimal GetCenaUzywanegoMinus1(int liczbaMiesiecy,out CalcReportNameValueTable reportTable, Dictionary<int, decimal> tabelaEurotax)
        {
            reportTable = null;
            if (liczbaMiesiecy == 0)
                return 1.0m;


            int miesiaceWTabeli = 0;
            int miesiaceWTabeli_2 = 0;
            bool czyInaczejLiczyc = false;
            if (liczbaMiesiecy < 18)
            {
                miesiaceWTabeli = 12;
                miesiaceWTabeli_2 = 24;
            }
            else if (liczbaMiesiecy < 30)
            {
                miesiaceWTabeli = 24;
                miesiaceWTabeli_2 = 36;
            }
            else if (liczbaMiesiecy < 42)
            {
                miesiaceWTabeli = 36;
                miesiaceWTabeli_2 = 48;
            }
            else if (liczbaMiesiecy < 54)
            {
                miesiaceWTabeli = 48;
                miesiaceWTabeli_2 = 60;
            }
            else if (liczbaMiesiecy <= 60)
            {
                miesiaceWTabeli = 60;
                miesiaceWTabeli_2 = 48;
                czyInaczejLiczyc = true;
            }
            else if (liczbaMiesiecy <= 72)
            {
                miesiaceWTabeli = 72;
                miesiaceWTabeli_2 = 60;
                czyInaczejLiczyc = true;
            }

            decimal procent1, procent2;

            if (tabelaEurotax == null)
            {
                TabelaEuroTax tabela = _tabele.TabeleEurotax
                   .FirstOrDefault(t => t.SymbolEuroTaxId == _kalkulacja.Model.SymbolEuroTax.Id &&
                                        t.MarkaId == _kalkulacja.Model.Marka.Id);

                if (null == tabela)
                {
                    string symbol = _kalkulacja.Model.SymbolEuroTax.Symbol;
                    if (symbol != null && symbol.Length > 2)
                    {
                        string symbolEurotaxBase = _kalkulacja.Model.SymbolEuroTax.Symbol.Substring(0, 2);
                        tabela = _tabele.TabeleEurotax
                            .FirstOrDefault(t => t.SymbolEuroTax.Symbol == symbolEurotaxBase &&
                                            t.MarkaId == _kalkulacja.Model.Marka.Id);
                    }
                }

                if (null == tabela)
                    throw new Exception("Brak pozycji w TabelaEurotax");

                WpisTabelaEuroTax wpis1 = tabela.WpisTabelaEuroTaxes.Where(w => w.Miesiac == miesiaceWTabeli).FirstOrDefault();
                WpisTabelaEuroTax wpis2 = tabela.WpisTabelaEuroTaxes.Where(w => w.Miesiac == miesiaceWTabeli_2).FirstOrDefault();

                if (null == wpis1)
                    throw new Exception(String.Format("Brak pozycji w WpisTabelaEuroTax dla miesiąca {0}", miesiaceWTabeli)); ;

                if (null == wpis2)
                    throw new Exception(String.Format("Brak pozycji w WpisTabelaEuroTax dla miesiąca {0}", miesiaceWTabeli_2)); ;

                procent1 = wpis1.WartoscProcent;
                procent2 = wpis2.WartoscProcent;

                if (_tabele.IsReport)
                {
                    reportTable = new CalcReportNameValueTable("cenaUzywanegoMinus1");
                    reportTable.AddValue("miesiaceWTabeli", miesiaceWTabeli);
                    reportTable.AddValue("miesiaceWTabeli_2", miesiaceWTabeli_2);
                    reportTable.AddValue("tabela.MarkaId", tabela.MarkaId);
                    reportTable.AddValue("wpis1", procent1);
                    reportTable.AddValue("wpis2", procent2);
                }
                else
                    reportTable = null;
            } else
            {
                procent1 = tabelaEurotax[miesiaceWTabeli];
                procent2 = tabelaEurotax[miesiaceWTabeli_2];
            }


            if (!czyInaczejLiczyc)
            {
                decimal cenaUzywanego = ((procent1 + procent2) / 2);
                return cenaUzywanego;
            }
            else
            {
                decimal cenaUzywanego = (2 * procent1 - procent2);
                return cenaUzywanego;
            }

        }

        private decimal GetCenaUzywanego(int liczbaMiesiecy, Dictionary<int, decimal> tabelaEurotax)
        {

            if (liczbaMiesiecy == 0)
                return 1.0m;
           
            int miesiaceWTabeli = 72;
            if (liczbaMiesiecy < 18) miesiaceWTabeli = 12;
            else if (liczbaMiesiecy < 30) miesiaceWTabeli = 24;
            else if (liczbaMiesiecy < 42) miesiaceWTabeli = 36;
            else if (liczbaMiesiecy < 54) miesiaceWTabeli = 48;
            else if (liczbaMiesiecy <= 60) miesiaceWTabeli = 60;

            decimal procent;
            if (tabelaEurotax == null)
            {
                TabelaEuroTax tabela = _tabele.TabeleEurotax
                    .FirstOrDefault(t => t.SymbolEuroTaxId == _kalkulacja.Model.SymbolEuroTax.Id &&
                                         t.MarkaId == _kalkulacja.Model.Marka.Id);


                if (null == tabela)
                {
                    string symbol = _kalkulacja.Model.SymbolEuroTax.Symbol;
                    if (symbol != null && symbol.Length > 2)
                    {
                        string symbolEurotaxBase = _kalkulacja.Model.SymbolEuroTax.Symbol.Substring(0, 2);
                        tabela = _tabele.TabeleEurotax.FirstOrDefault(t => t.SymbolEuroTax.Symbol == symbolEurotaxBase);
                    }
                }

                if (null == tabela)
                    throw new Exception("Brak pozycji w TabelaEuroTax");

                WpisTabelaEuroTax wpis = tabela
                    .WpisTabelaEuroTaxes
                    .Where(w => w.Miesiac == miesiaceWTabeli)
                    .FirstOrDefault();

                if (null == wpis)
                    throw new Exception(string.Format("Brak pozycji w WpisTabelaEuroTax dla Miesiac={0}", miesiaceWTabeli));

                procent = wpis.WartoscProcent;

                if (_tabele.IsReport)
                {

                    CalcReportNameValueTable table = new CalcReportNameValueTable("Tabela Eurotax");
                    table.UniqueName = CalcReportNameValueTable.UNIQUE_NAME_TABELA_EUROTAX;

                    table.AddValue("Symbol eurotax", (null != tabela.SymbolEuroTax) ? tabela.SymbolEuroTax.Symbol : tabela.SymbolEuroTaxId.ToString());
                    table.AddValue("Marka", (null != tabela.Marka) ? tabela.Marka.Nazwa : tabela.MarkaId.ToString());

                    var wpisy = tabela.WpisTabelaEuroTaxes.OrderBy(w => w.Miesiac);
                    foreach (WpisTabelaEuroTax w in wpisy)
                        table.AddValue(w.Miesiac.ToString(), w.WartoscProcent.ToString());

                    _tabele.Report.AddItem(table);

                }
            } else
            {
                procent = tabelaEurotax[miesiaceWTabeli];
            }

            return  procent;
        }

        private decimal GetKorektaAdministracyjna(decimal wrPoKorekcieZaPrzebieg, out CalcReportNameValueTable reportTable)
        {

            int symboEuroTaxId = _kalkulacja.Model.SymbolEuroTax.Id;
            Model model = _kalkulacja.Model;

            decimal korektaNaukaJazdy = 0;
            if (_kalkulacja.NaukaJazdy.IsTrue())
                korektaNaukaJazdy = _tabele.Parametry.KorektaNaukaJazdy;

            LTRAdminKorektyRvDsu pozycja = _tabele.KorektyRvDsu
                .FirstOrDefault(p => p.SymbolEuroTax.Id == symboEuroTaxId);


            if (null == pozycja)
            {
                string symbol = _kalkulacja.Model.SymbolEuroTax.Symbol;
                if (symbol != null && symbol.Length > 2)
                {
                    string symbolEurotaxBase = _kalkulacja.Model.SymbolEuroTax.Symbol.Substring(0, 2);
                    pozycja = _tabele.KorektyRvDsu.FirstOrDefault(t => t.SymbolEuroTax.Symbol == symbolEurotaxBase);
                }
            }

            if (null == pozycja)
                throw new Exception("Brak pozycji w tabeli LTRAdminKorektyRvDsu");

            decimal korektaMetalik = 0;
            if (_kalkulacja.Metalik.IsFalse())
            {
                korektaMetalik += (pozycja.BrakMetalika ?? 0.0m);
            }

            decimal korektaKombi = 0;
            if (!string.IsNullOrEmpty(_kalkulacja.RodzajNadwozia))
            {
                if (_kalkulacja.RodzajNadwozia.ToUpper().Contains("KOMBI"))
                    korektaKombi = Kwota.GetAsDecimal(pozycja.Kombi) / wrPoKorekcieZaPrzebieg;
            }


            if (_tabele.IsReport)
            {
                reportTable = new CalcReportNameValueTable("korektaAdministracyjna");
                reportTable.AddValue("korektaNaukaJazdy", korektaNaukaJazdy);
                reportTable.AddValue("symbolEtaxId", pozycja.SymbolEuroTaxId);
                if (null != pozycja.SymbolEuroTax)
                    reportTable.AddValue("symbolEtax", pozycja.SymbolEuroTax.Symbol);
                reportTable.AddValue("korektaMetalik", korektaMetalik);
                reportTable.AddValue("korektaKombi", korektaKombi);
            }
            else
                reportTable = null;

            decimal korekta = korektaNaukaJazdy + korektaMetalik + korektaKombi;
            return korekta;

        }

        private decimal GetKorektaZaPrzebieg(decimal cenaUzywanegoBezKorekty, decimal nadprzebiegNiedobieg, out CalcReportNameValueTable reportTable)
        {

            if (null == _kalkulacja.CzynszInicjalny)
                throw new Exception("Brak parametru CzynszInicjalny w Kalkulacji");

            int przebiegKoncowy = _tabele.Przebieg;

            var pozycja = _tabele.KorektyEtax.Where(k => k.SymbolEuroTax.Id == _kalkulacja.Model.SymbolEuroTax.Id).FirstOrDefault();

            if (null == pozycja)
            {
                string symbol = _kalkulacja.Model.SymbolEuroTax.Symbol;
                if (symbol != null && symbol.Length > 2)
                {
                    string symbolEurotaxBase = _kalkulacja.Model.SymbolEuroTax.Symbol.Substring(0, 2);
                    pozycja = _tabele.KorektyEtax.FirstOrDefault(t => t.SymbolEuroTax.Symbol == symbolEurotaxBase);
                }
            }

            if (null == pozycja)
                throw new Exception("Brak pozycji w LTRKorektaEtax");

            decimal korekta = 0;
            string typKorekty = string.Empty;
            if (nadprzebiegNiedobieg <= 0 && przebiegKoncowy <= 100000)
            {
                typKorekty = "dodatnia <100";
                korekta = pozycja.KorektaDodatnia1.Value;
            }
            else if (nadprzebiegNiedobieg <= 0 && przebiegKoncowy >= 100000)
            {
                typKorekty = "dodatnia >100";
                korekta = pozycja.KorektaDodatnia2.Value;
            }
            else if (nadprzebiegNiedobieg >= 0 && przebiegKoncowy <= 100000)
            {
                typKorekty = "ujemna <100";
                korekta = pozycja.KorektaUjemna1.Value;
            }
            else if (nadprzebiegNiedobieg >= 0 && przebiegKoncowy >= 100000)
            {
                typKorekty = "ujemna >100";
                korekta = pozycja.KorektaUjemna2.Value;
            }


            if (_tabele.IsReport)
            {
                reportTable = new CalcReportNameValueTable("korektaZaPrzebieg");
                reportTable.AddValue("przebiegKoncowy", przebiegKoncowy);
                reportTable.AddValue("symbolEtaxId", pozycja.SymbolEuroTaxId);
                if (null != pozycja.SymbolEuroTax)
                    reportTable.AddValue("symbolEtax", pozycja.SymbolEuroTax.Symbol);
                reportTable.AddValue("typKorekty", typKorekty);
                reportTable.AddValue("korekta", korekta);
            }
            else
                reportTable = null;


            return cenaUzywanegoBezKorekty * korekta * (nadprzebiegNiedobieg / 100000);
        }

        private decimal GetPrzebiegNormatywny(int miesiacStart,out CalcReportNameValueTable reportTable)
        {

            int liczbaMiesiecy = _tabele.Okres + miesiacStart;

            int miesiaceWTabeli = 72;
            if (liczbaMiesiecy < 18) miesiaceWTabeli = 12;
            else if (liczbaMiesiecy < 30) miesiaceWTabeli = 24;
            else if (liczbaMiesiecy < 42) miesiaceWTabeli = 36;
            else if (liczbaMiesiecy < 54) miesiaceWTabeli = 48;
            else if (liczbaMiesiecy <= 60) miesiaceWTabeli = 60;

            int symbolEuroTax = _kalkulacja.Model.SymbolEuroTax.Id;

            var pozycja = _tabele.PrzebiegNormatywny
                .Where(p => p.SymbolEuroTaxId == symbolEuroTax && p.LiczbaMiesiecy == miesiaceWTabeli)
                .FirstOrDefault();

            if (null == pozycja)
            {
                string symbol = _kalkulacja.Model.SymbolEuroTax.Symbol;
                if (symbol != null && symbol.Length > 2)
                {
                    string symbolEurotaxBase = _kalkulacja.Model.SymbolEuroTax.Symbol.Substring(0, 2);
                    pozycja = _tabele.PrzebiegNormatywny.FirstOrDefault(w => w.SymbolEuroTax.Symbol == symbolEurotaxBase && w.LiczbaMiesiecy == miesiaceWTabeli);
                }
            }



            if (null == pozycja)
                throw new Exception("Brak pozycji w tabeli LTRPrzebiegNormatywny");


            decimal przebieg = ((decimal)pozycja.Przebieg.Value) / ((decimal)miesiaceWTabeli);

            if (_tabele.IsReport)
            {
                reportTable = new CalcReportNameValueTable("przebieg normatywny");
                reportTable.AddValue("miesiaceWTabeli", miesiaceWTabeli);
                reportTable.AddValue("symbolEuroTaxId", pozycja.SymbolEuroTaxId);

                if (null != pozycja.SymbolEuroTax)
                    reportTable.AddValue("symbolEuroTax", pozycja.SymbolEuroTax.Symbol);

                reportTable.AddValue("przebieg", pozycja.Przebieg);

            }
            else
                reportTable = null;

            return przebieg;

        }

        private decimal GetKorektaRVDlaLTR()
        {
            if (_kalkulacja.Model.BrakWsparciaLTR ?? false)
                return 0.0m;

            if (_kalkulacja.LiniaProduktowaId == null || _kalkulacja.LiniaProduktowaId != LiniaProduktowaConst.LTRId)
                return 0.0m;

            string klasa = _kalkulacja.Model != null ? _kalkulacja.Model.Klasa != null ? _kalkulacja.Model.Klasa.Nazwa : string.Empty : string.Empty;

            // co robić w przypadku, gdzy klasa byłaby pusta?
            // Rozwiązanie na ten moment: wywalenie wyjątku
            if (klasa == string.Empty)
                throw new Exception("Pobranie korekty RV dla LTR niemożliwe, gdyż brak danych dotyczących klasy.");

            string key = klasa + " - " + _kalkulacja.RodzajPaliwa;
            WRLTR korektaEntity = _tabele.WRLTRTable.FirstOrDefault(e => e.Nazwa == key);
            if (korektaEntity == null)
                return 0.0M;

            return korektaEntity.KorektaRVNetto.GetValue();
        }

        private decimal GetWRPrzewidywanaCenaSprzedazy(decimal wrPoKorekcieZaPrzebieg, decimal korektaAdministracyjna, decimal korektaRVDlaLTR, decimal korektaWR)
        {
            decimal result = 0;
            if (_kalkulacja.CzyUwzgledniaSerwisowanie ?? false)
                result = wrPoKorekcieZaPrzebieg * (1 - korektaAdministracyjna) + korektaRVDlaLTR;

            else
            {
                result = wrPoKorekcieZaPrzebieg * (1 - korektaAdministracyjna) + korektaRVDlaLTR;
                result *= (1 + _tabele.Parametry.UtrataWartosciPrzyBrakuSerwisowania);
            }

            result += korektaWR / _tabele.Parametry.StawkaVAT;
            return result;
        }
    }

}
