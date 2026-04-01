using Express.Logic.Kalkulatory.LTR.CalculatorReport;
using Express.Logic.Kalkulatory.LTR.Calculators;
using Express.Logic.Kalkulatory.LTR.CalculatorTypes;
using Express.Types.Magazyny;
using Express.WD;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TF.BusinessFramework.Patterns.UnitOfWork;
using TF.BusinessFramework.SW.Interfaces;

namespace Express.Logic.Kalkulatory.LTR
{
    public class LTRKalkulator
    {

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        #region <public nested types>

        public class Koszt
        {
            public decimal RozkladMarzy { get; set; }
            public decimal KwotaMarzy { get; set; }
            public decimal KosztPlusMarza { get; set; }
            public decimal RozkladMarzyKorekta { get; set; }
            public decimal KwotaMarzyKorekta { get; set; }
            public decimal KosztPlusMarzaKorekta { get; set; }
        }

        public class Result
        {
            public decimal CzynszInicjalnyProcent { get; set; }
            public decimal CzynszInicjalnyNetto { get; set; }
            public decimal PodstawaMarzy { get; set; }
            public decimal LacznaStawka { get; set; }
            public decimal CzynszFinansowy { get; set; }
            public decimal CzynszTechniczny { get; set; }
            public decimal Ubezpieczenie { get; set; }
            public decimal Serwis { get; set; }
            public decimal Admin { get; set; }
            public decimal Opony { get; set; }
            public decimal SamochodZastepczy { get; set; }
            public decimal CenaZakupu { get; set; }
            public decimal Cena1KompletOpon { get; set; }
            public decimal CenaZakupuBezOpon { get; set; }
            public decimal CenaZakupuBezOponIOpcjiSerwisowych { get; set; }
            public decimal CenaZakupuBezOponIOpcjiSerwisowychIPakietu { get; set; }
            public decimal LacznieKosztySerwisowe { get; set; }
            public decimal WR { get; set; }
            public decimal WRdlaLO { get; set; }
            public decimal KorektaWRMaks { get; set; }
            public decimal KosztDzienny { get; set; }
            public decimal Przychod { get; set; }
            public decimal KosztyOgolem { get; set; }
            public decimal MarzaMiesiac { get; set; }
            public decimal MarzaNaKontrakcie { get; set; }
            public decimal MarzaNaKontrakcieProcent { get; set; }
            public decimal AmortyzacjaProcent { get; set; }
            public decimal RabatKwotowo { get; set; }
            public decimal LacznyKosztOpon { get; set; }
            public decimal LacznieUbezpieczenie { get; set; }
            public decimal KosztySerwisowe { get; set; }
            public decimal KorektaZaPrzebiegKwotowo { get; set; }
            public decimal KorektaAdministracyjnaKwotowo { get; set; }
            public decimal UtrataWartosci { get; set; }
            public decimal LacznyKosztCzesciOdsetkowejRaty { get; set; }
            public decimal SumaOdsetekBezCzynszuInicjalnego { get; set; }
            public decimal IloscOpon { get; set; }
            public decimal KosztyDodatkowe { get; set; }
            public decimal LacznieSamochodZastepczy { get; set; }
            public decimal KosztyLaczneMC { get; set; }
            public decimal KosztFinansowyLacznie { get; set; }
            public decimal KosztFinansowyMiesiecznie { get; set; }

            public string ReportHtml { get; set; }

            public Koszt KosztFinansowy { get; set; }
            public Koszt KosztUbezpieczenie { get; set; }
            public Koszt KosztSamochodZastepczy { get; set; }
            public Koszt KosztSerwis { get; set; }
            public Koszt KosztOpony { get; set; }
            public Koszt KosztAdmin { get; set; }

        }
        #endregion

        #region <members>
        IUnitOfWork _unitOfWork;
        LTRTabele _tabele;
        Kalkulacja _kalkulacja;

        LTRSubCalculatorOpony _subOpony;
        LTRSubCalculatorKosztyDodatkowe _subKoszty;
        LTRSubCalculatorSamochodZastepczy _subZastepczy;
        LTRSubCalculatorSerwis _subSerwis;
        LTRSubCalculatorCenaZakupu _subCenaZakupu;
        LTRSubCalculatorUtrataWartosci _subUtrataWartosci;
        LTRSubCalculatorAmortyzacja _subAmortyzacja;
        LTRSubCalculatorUbezpieczenie _subUbezpieczenie;
        LTRSubCalculatorFinanse _subFinanse;
        LTRSubCalculatorKosztDzienny _subKosztDzienny;
        LTRSubCalculatorStawka _subStawka;
        LTRSubCalculatorBudzetMarketingowy _budzetMarketingowy;

        #endregion

        #region <constructor>
        public LTRKalkulator(IUnitOfWork unitOfWork, Kalkulacja kalkulacja, bool isReport = false)
        {
            CurrentSubcalculatorName = string.Empty;

            _unitOfWork = unitOfWork;
            _kalkulacja = kalkulacja;
            InitTables(kalkulacja, isReport);
            CheckData(kalkulacja, _tabele);
            updateWiborIMarza(kalkulacja);
        }

        void InitTables(Kalkulacja kalkulacja, bool isReport)
        {
            var korektyEtax = _unitOfWork.GetRepository<LTRKorektaEtax>().GetAll();
            var korektyRv = _unitOfWork.GetRepository<LTRAdminKorektyRvDsu>().GetAll();
            var oplatyTransportowe = _unitOfWork.GetRepository<LTRAdminOplatyTransportowe>().GetAll();
            var parameters = _unitOfWork.GetRepository<LTRAdminParametry>().GetAll().ToList();
            var przebiegNormatywny = _unitOfWork.GetRepository<LTRPrzebiegNormatywny>().GetAll();
            var ubezpieczenie = _unitOfWork.GetRepository<LTRAdminUbezpieczenie>().GetAll();
            var wspolczynnikiSzkodowe = _unitOfWork.GetRepository<LTRAdminWspolczynnikiSzkodowe>().GetAll();
            var abonamentGSM = _unitOfWork.GetRepository<LTRAdminGSM>().GetAll();
            var samochodZastepczy = _unitOfWork.GetRepository<LTRAdminStawkaZastepczy>().GetAll();

            int cennikOponId = _unitOfWork.GetRepository<CennikOpon>().AsQueryable().Where(c => c.Uwagi == "LTR").OrderByDescending(c => c.Id).Select(c => c.Id).FirstOrDefault();
            if (cennikOponId == 0) cennikOponId = _unitOfWork.GetRepository<CennikOpon>().AsQueryable().OrderByDescending(c => c.Id).Select(c => c.Id).FirstOrDefault();

            // var tabelaserwisowa = _unitOfWork.GetRepository<TabelaSerwisowa>().GetAll();
            var eurotax = _unitOfWork.GetRepository<TabelaEuroTax>().GetAll();
            var kategoriaKorekta = _unitOfWork.GetRepository<LTRAdminKategoriaKorekta>().GetAll();
            var przebiegKorekta = _unitOfWork.GetRepository<LTRAdminPrzebiegKorekta>().GetAll();
            var kosztZabudowy = _unitOfWork.GetRepository<LTRAdminKosztZabudowy>().GetAll();
            var WRLTRtable = _unitOfWork.GetRepository<WRLTR>().GetAll();

            Dictionary<string, string> paramDict = new Dictionary<string, string>();
            parameters.ForEach(p => paramDict.Add(p.Nazwa, p.Wartosc));
            LTRParametry parametry = new LTRParametry(paramDict);

            _tabele = new LTRTabele(_unitOfWork)
            {
                AbonamentGSM = abonamentGSM,
                CennikOponId = cennikOponId,
                KorektyEtax = korektyEtax,
                KorektyRvDsu = korektyRv,
                OplatyTransportowe = oplatyTransportowe,
                Parametry = parametry,
                PrzebiegNormatywny = przebiegNormatywny,
                StawkaZastepczy = samochodZastepczy,

                TabeleEurotax = eurotax,
                Ubezpieczenie = ubezpieczenie,
                WspolczynnikiSzkodowe = wspolczynnikiSzkodowe,
                KategoriaKorekta = kategoriaKorekta,
                PrzebiegKorekta = przebiegKorekta,
                KosztZabudowy = kosztZabudowy,
                WRLTRTable = WRLTRtable,
                Report = new CalcReport(),
                IsReport = isReport
            };

            _subOpony = new LTRSubCalculatorOpony(kalkulacja, _tabele);
            _subKoszty = new LTRSubCalculatorKosztyDodatkowe(kalkulacja, _tabele);
            _subZastepczy = new LTRSubCalculatorSamochodZastepczy(kalkulacja, _tabele);
            _subSerwis = new LTRSubCalculatorSerwis(kalkulacja, _tabele);
            _subCenaZakupu = new LTRSubCalculatorCenaZakupu(kalkulacja, _tabele);
            _subUtrataWartosci = new LTRSubCalculatorUtrataWartosci(kalkulacja, _tabele);
            _subAmortyzacja = new LTRSubCalculatorAmortyzacja(kalkulacja, _tabele);
            _subUbezpieczenie = new LTRSubCalculatorUbezpieczenie(kalkulacja, _tabele);
            _subFinanse = new LTRSubCalculatorFinanse(kalkulacja, _tabele);
            _subKosztDzienny = new LTRSubCalculatorKosztDzienny(kalkulacja, _tabele);
            _subStawka = new LTRSubCalculatorStawka(kalkulacja, _tabele);
            _budzetMarketingowy = new LTRSubCalculatorBudzetMarketingowy(kalkulacja, _tabele);

            if (isReport)
            {
                AddInputDataToReport(kalkulacja);
            }
        }

        void CheckData(Kalkulacja kalkulacja, LTRTabele tabele)
        {
            List<string> messages = new List<string>();

            if (null == kalkulacja.Model)
                messages.Add("Brak modelu samochodu w kalkulacji");

            if (null == kalkulacja.Model.SymbolEuroTax)
                messages.Add("Brak symbolu eurotax w modelu samochodu w kalkulacji");

            if (tabele.Parametry.StawkaVAT == 0m)
                messages.Add("Zerowa stawka VAT");

            if (tabele.CennikOponId == 0)
                messages.Add("Brak cennika opon");

            if (Kwota.IsNullOrZero(kalkulacja.CenaCennikowa))
                messages.Add("Cena cennikowa jest równa 0");

            if (messages.Count > 0)
            {
                string message = string.Join("; ", messages);
                throw new Exception(message);
            }

        }

        void updateWiborIMarza(Kalkulacja kalkulacja)
        {
            if (!kalkulacja.WIBORProcent.HasValue)
            {
                kalkulacja.WIBORProcent = _tabele.Parametry.WIBOR;
            }

            if (!kalkulacja.MarzaFinansowaProcent.HasValue)
            {
                kalkulacja.MarzaFinansowaProcent = _tabele.Parametry.MarzaFinansowa;
            }
        }
        #endregion

        #region <properties>
        public string CurrentSubcalculatorName { get; private set; }
        #endregion

        #region <public methods>

        public Result Calculate(int przebieg, int okres, StarySamochodDane starySamochodDane)
        {
            return Calculate(przebieg, okres, starySamochodDane, false);
        }

        public Result Calculate(int przebieg, int okres, StarySamochodDane starySamochodDane, bool isAP)
        {
            _tabele.Przebieg = przebieg;
            _tabele.PrzebiegSkorygowany = GetPrzebiegForLiniaProduktowa(przebieg);
            _tabele.Okres = okres;

            var APWR12Months = isAP ? _tabele.Parametry.APWR12Months : (decimal?)null;

            CurrentSubcalculatorName = "(Op)";
            LTRSubCalculatorOpony.Result subOponyResult =
                _subOpony.Policz(new LTRSubCalculatorOpony.Data() { StarySamochod = starySamochodDane });

            CurrentSubcalculatorName = "(KDod)";
            LTRSubCalculatorKosztyDodatkowe.Result subKosztyDodatkoweResult =
                _subKoszty.Policz(new LTRSubCalculatorKosztyDodatkowe.Data() { StarySamochod = starySamochodDane });

            CurrentSubcalculatorName = "(SZst)";
            LTRSubCalculatorSamochodZastepczy.Result subZastepczyResult =
                _subZastepczy.Policz(new LTRSubCalculatorSamochodZastepczy.Data() { StarySamochod = starySamochodDane });

            CurrentSubcalculatorName = "(Srw)";
            LTRSubCalculatorSerwisNewOutput subSerwisResult;
            if (_tabele.Parametry.UzywajNowegoKalkulatoraSerwis)
            {
                LTRSubCalculatorSerwisNew _subSerwisNew = new LTRSubCalculatorSerwisNew();
                LTRSubCalculatorSerwisNewInput calculatorSerwisNewInput = new LTRSubCalculatorSerwisNewInput
                {
                    OkresUzytkowania = okres,
                    Przebieg = przebieg,
                    IsReport = _tabele.IsReport,
                    PrzebiegPoczatkowy = starySamochodDane != null ? starySamochodDane.PrzebiegPoczatkowy : 0
                };
                subSerwisResult = _subSerwisNew.Calculate(_kalkulacja, calculatorSerwisNewInput, _tabele.Report, _unitOfWork);
            }
            else
            {
                //tu jest tylko typ zmieniony żeby się zgadzał ale interfejs jest ten sam w nowym i starym
                subSerwisResult = _subSerwis.Policz(new LTRSubCalculatorSerwis.Data() { PrzebiegSkorygowany = _tabele.PrzebiegSkorygowany, StarySamochod = starySamochodDane });
            }


                

            CurrentSubcalculatorName = "(CeZ)";
            LTRSubCalculatorCenaZakupu.Data subCenaZakupuData = new LTRSubCalculatorCenaZakupu.Data()
            { Koszt1KompletOpon = subOponyResult.Koszt1KplOpon, StarySamochod = starySamochodDane };
            LTRSubCalculatorCenaZakupu.Result subCenaZakupuResult = (LTRSubCalculatorCenaZakupu.Result)
                _subCenaZakupu.Policz(subCenaZakupuData);

            CurrentSubcalculatorName = "(UtW)";
            LTRSubCalculatorUtrataWartosciNewOutput subUtrataWartosciResult;
            if (_tabele.Parametry.UzywajNowegoKalkulatoraWR)
            {
                LTRSubCalculatorUtrataWartosciNew _subUtrataWartosciNew = new LTRSubCalculatorUtrataWartosciNew();
                LTRSubCalculatorUtrataWartosciNewInput calculatorUtrataWartosciNewInput = new LTRSubCalculatorUtrataWartosciNewInput
                {
                    OkresUzytkowania = okres,
                    Przebieg = przebieg,
                    IsReport = _tabele.IsReport
                };
                subUtrataWartosciResult = _subUtrataWartosciNew.Calculate(_kalkulacja, calculatorUtrataWartosciNewInput, _tabele.Report, _unitOfWork);
            }
            else
            {
                LTRSubCalculatorUtrataWartosci.Data subUtrataWartosciData = new LTRSubCalculatorUtrataWartosci.Data()
                {
                    CenaZakupuBezOponOpcjiSerwisowychIpakietuSerwisowego = subCenaZakupuResult.CenaSamochoduBezOpon_OpcjiSerwisowych_iPakietuNetto,
                    StarySamochod = starySamochodDane,
                    TabelaEurotax = APWR12Months.HasValue ? new Dictionary<int, decimal> {
                        {12, APWR12Months.GetValue() },
                        {24, 2m * APWR12Months.GetValue() },
                        {36, 3m * APWR12Months.GetValue() },
                        {48, 4m * APWR12Months.GetValue() },
                        {60, 5m * APWR12Months.GetValue() },
                        {72, 6m * APWR12Months.GetValue() },
                    } : null,
                };
                //tu jest tylko typ zmieniony żeby się zgadzał ale interfejs jest ten sam w nowym i starym
                subUtrataWartosciResult = _subUtrataWartosci.Policz(subUtrataWartosciData);

            }


            CurrentSubcalculatorName = "(Am)";
            LTRSubCalculatorAmortyzacja.Data subAmortyzacjaData = new LTRSubCalculatorAmortyzacja.Data()
            {
                WP = subCenaZakupuResult.CenaZakupu,
                WR = subUtrataWartosciResult.WR,
                StarySamochod = starySamochodDane 
            };
            LTRSubCalculatorAmortyzacja.Result subAmortyzacjaResult =
                _subAmortyzacja.Policz(subAmortyzacjaData);

            CurrentSubcalculatorName = "(Ub)";
            LTRSubCalculatorUbezpieczenie.Data subUbezpieczenieData = new LTRSubCalculatorUbezpieczenie.Data()
            {
                AmortyzacjaProcent = subAmortyzacjaResult.AmortyzacjaProcent,
                CenaZakupu = subCenaZakupuResult.CenaZakupu,
                StarySamochod = starySamochodDane 
            };
            LTRSubCalculatorUbezpieczenie.Result subUbezpieczenieResult =
                _subUbezpieczenie.Policz(subUbezpieczenieData);

            CurrentSubcalculatorName = "(Fi)";
            LTRSubCalculatorFinanse.Data subFinanseData = new LTRSubCalculatorFinanse.Data()
            {
                WartoscPoczatkowaNetto = subCenaZakupuResult.CenaZakupu,
                WrPrzewidywanaCenaSprzedazy = subUtrataWartosciResult.WR,
                StarySamochod = starySamochodDane 

            };
            LTRSubCalculatorFinanse.Result subFinanseResult =
                _subFinanse.Policz(subFinanseData);

            CurrentSubcalculatorName = "(KDz)";
            LTRSubCalculatorKosztDzienny.Data subKosztDziennyData = new LTRSubCalculatorKosztDzienny.Data()
            {
                UtrataWartosciZczynszem = subUtrataWartosciResult.UtrataWartosciZCzynszemInicjalnym,
                UtrataWartosciBEZczynszu = subUtrataWartosciResult.UtrataWartosciBEZczynszu,
                KosztFinansowy = subFinanseResult.SumaOdsetekZczynszem,
                SamochodZastepczyNetto = subZastepczyResult.StawkaZaZastepczyNetto,
                KosztyDodatkoweNetto = subKosztyDodatkoweResult.KosztyDodatkowe,
                UbezpieczenieNetto = subUbezpieczenieResult != null ? subUbezpieczenieResult.SkladkaWCalymOkresieNetto : 0,
                OponyNetto = subOponyResult.OponyNetto,
                SerwisNetto = subSerwisResult.SerwisNetto,
                SumaOdsetekBezCzynszuInicjalnego = subFinanseResult.SumaOdsetekBEZczynszu,
                StarySamochod = starySamochodDane 
            };
            LTRSubCalculatorKosztDzienny.Result subKoszDziennyResult =
                _subKosztDzienny.Policz(subKosztDziennyData);

            CurrentSubcalculatorName = "(St)";
            LTRSubCalculatorStawka.Data subStawkaData = new LTRSubCalculatorStawka.Data()
            {
                KosztMC = subKoszDziennyResult.KosztMC,
                KosztMcBEZcz = subKoszDziennyResult.KosztMcBEZcz,
                UtrataWartosciNetto = subUtrataWartosciResult.UtrataWartosciZCzynszemInicjalnym,
                KosztyFinansoweNetto = subFinanseResult.SumaOdsetekZczynszem,
                UbezpieczenieNetto = subUbezpieczenieResult.SkladkaWCalymOkresieNetto,
                SamochodZastepczyNetto = subZastepczyResult.StawkaZaZastepczyNetto,
                KosztyDodatkoweNetto = subKosztyDodatkoweResult.KosztyDodatkowe,
                OponyNetto = subOponyResult.OponyNetto,
                SerwisNetto = subSerwisResult.SerwisNetto,
                StarySamochod = starySamochodDane 
            };
            LTRSubCalculatorStawka.Result subStawkaResult =
                _subStawka.Policz(subStawkaData);

            CurrentSubcalculatorName = "(Bm)";
            LTRSubCalculatorBudzetMarketingowy.Data subBudzetMarketingowyData = new LTRSubCalculatorBudzetMarketingowy.Data()
            {
                UtrataWartosciZCzynszemInicjalnym = subUtrataWartosciResult.UtrataWartosciZCzynszemInicjalnym,
                WRPrzewidywanaCenaSprzedazy = subUtrataWartosciResult.WR,
                StarySamochod = starySamochodDane 
            };
            LTRSubCalculatorBudzetMarketingowy.Result subBudzetMarketingowyResult =
                _budzetMarketingowy.Policz(subBudzetMarketingowyData);


            CurrentSubcalculatorName = string.Empty;
            Result result = new Result()
            {
                CzynszInicjalnyProcent = subFinanseResult.CzynszInicjalnyProcent,
                CzynszInicjalnyNetto = Math.Round(subFinanseResult.CzynszInicjalnyNetto),
                PodstawaMarzy = subStawkaResult.PodstawaMarzy,
                LacznaStawka = Math.Round(subStawkaResult.OferowanaStawka),
                CzynszFinansowy = Math.Round(subStawkaResult.CzynszFinansowy),
                CzynszTechniczny = Math.Round(subStawkaResult.CzynszTechniczny),
                Ubezpieczenie = Math.Round(subStawkaResult.Ubezpieczenie),
                Serwis = Math.Round(subStawkaResult.Serwis),
                Admin = Math.Round(subStawkaResult.Admin),
                Opony = Math.Round(subStawkaResult.Opony),
                SamochodZastepczy = Math.Round(subStawkaResult.SamochodZastepczy),
                LacznieSamochodZastepczy = Math.Round(subZastepczyResult.StawkaZaZastepczyNetto),
                CenaZakupu = subCenaZakupuResult.CenaZakupu,
                Cena1KompletOpon = Math.Round(subOponyResult.Koszt1KplOpon),
                CenaZakupuBezOpon = subCenaZakupuResult.CenaZakupuBezOpon,
                CenaZakupuBezOponIOpcjiSerwisowych = subCenaZakupuResult.CenaZakupuBezOponIOpcjiSerwisowych,
                CenaZakupuBezOponIOpcjiSerwisowychIPakietu = subCenaZakupuResult.CenaSamochoduBezOpon_OpcjiSerwisowych_iPakietuNetto,
                RabatKwotowo = subCenaZakupuResult.RabatKwotowo,
                LacznieKosztySerwisowe = Math.Round(subSerwisResult.SerwisNetto),
                WR = Math.Round(subUtrataWartosciResult.WR),
                WRdlaLO = Math.Round(subUtrataWartosciResult.WRdlaLO),
                KorektaWRMaks = Math.Round(subBudzetMarketingowyResult.KorektaWRMaks, 2),
                KosztDzienny = Math.Round(subKoszDziennyResult.KosztDzienny, 2),
                Przychod = Math.Round(subStawkaResult.Przychod),
                KosztyOgolem = Math.Round(subKoszDziennyResult.KosztyOgolem),
                MarzaMiesiac = Math.Round(subStawkaResult.MarzaMC),
                MarzaNaKontrakcie = Math.Round(subStawkaResult.MarzaNaKontrakcie),
                //tu celowo nie ma Math.Round - proszę nie ruszać bez potrzeby :)
                MarzaNaKontrakcieProcent = subStawkaResult.MarzaNaKontrakcieProcent,
                //tu celowo nie ma Math.Round - proszę nie ruszać bez potrzeby :)
                AmortyzacjaProcent = subAmortyzacjaResult.AmortyzacjaProcent,
                LacznyKosztOpon = Math.Round(subOponyResult.OponyNetto),
                LacznieUbezpieczenie = Math.Round(subUbezpieczenieResult.SkladkaWCalymOkresieNetto),
                KosztySerwisowe = Math.Round(subSerwisResult.SerwisNetto),
                KorektaZaPrzebiegKwotowo = Math.Round(subUtrataWartosciResult.KorektaZaPrzebiegKwotowo),
                KorektaAdministracyjnaKwotowo = Math.Round(subUtrataWartosciResult.KorektaAdministracyjnaKwotowo),
                UtrataWartosci = Math.Round(subUtrataWartosciResult.UtrataWartosciZCzynszemInicjalnym),
                LacznyKosztCzesciOdsetkowejRaty = Math.Round(subFinanseResult.SumaOdsetekZczynszem),
                SumaOdsetekBezCzynszuInicjalnego = Math.Round(subFinanseResult.SumaOdsetekBEZczynszu),
                IloscOpon = Math.Round(subOponyResult.IloscOpon),
                KosztyDodatkowe = Math.Round(subKosztyDodatkoweResult.KosztyDodatkowe),
                KosztyLaczneMC = subStawkaResult.KosztyLaczneMC,
                KosztFinansowyLacznie = Math.Round(subStawkaResult.KosztFinansowyLacznie),
                KosztFinansowyMiesiecznie = Math.Round(subStawkaResult.KosztFinansowyMiesiecznie),

                // na rozkład marży nie ma zaokrągleń ponieważ to są procenty
                KosztFinansowy = new Koszt
                {
                    RozkladMarzy = subStawkaResult.KosztFinansowy.RozkladMarzy,
                    RozkladMarzyKorekta = subStawkaResult.KosztFinansowy.RozkladMarzyKorekta,
                    KwotaMarzy = Math.Round(subStawkaResult.KosztFinansowy.KwotaMarzy),
                    KwotaMarzyKorekta = Math.Round(subStawkaResult.KosztFinansowy.KwotaMarzyKorekta),
                    KosztPlusMarza = Math.Round(subStawkaResult.KosztFinansowy.KosztPlusMarza),
                    KosztPlusMarzaKorekta = Math.Round(subStawkaResult.KosztFinansowy.KosztPlusMarzaKorekta)
                },

                KosztUbezpieczenie = new Koszt
                {
                    RozkladMarzy = subStawkaResult.KosztUbezpieczenie.RozkladMarzy,
                    RozkladMarzyKorekta = subStawkaResult.KosztUbezpieczenie.RozkladMarzyKorekta,
                    KwotaMarzy = Math.Round(subStawkaResult.KosztUbezpieczenie.KwotaMarzy),
                    KwotaMarzyKorekta = Math.Round(subStawkaResult.KosztUbezpieczenie.KwotaMarzyKorekta),
                    KosztPlusMarza = Math.Round(subStawkaResult.KosztUbezpieczenie.KosztPlusMarza),
                    KosztPlusMarzaKorekta = Math.Round(subStawkaResult.KosztUbezpieczenie.KosztPlusMarzaKorekta)
                },

                KosztOpony = new Koszt
                {
                    RozkladMarzy = subStawkaResult.KosztOpony.RozkladMarzy,
                    RozkladMarzyKorekta = subStawkaResult.KosztOpony.RozkladMarzyKorekta,
                    KwotaMarzy = Math.Round(subStawkaResult.KosztOpony.KwotaMarzy),
                    KwotaMarzyKorekta = Math.Round(subStawkaResult.KosztOpony.KwotaMarzyKorekta),
                    KosztPlusMarza = Math.Round(subStawkaResult.KosztOpony.KosztPlusMarza),
                    KosztPlusMarzaKorekta = Math.Round(subStawkaResult.KosztOpony.KosztPlusMarzaKorekta)
                },

                KosztSamochodZastepczy = new Koszt
                {
                    RozkladMarzy = subStawkaResult.KosztSamochodZastepczy.RozkladMarzy,
                    RozkladMarzyKorekta = subStawkaResult.KosztSamochodZastepczy.RozkladMarzyKorekta,
                    KwotaMarzy = Math.Round(subStawkaResult.KosztSamochodZastepczy.KwotaMarzy),
                    KwotaMarzyKorekta = Math.Round(subStawkaResult.KosztSamochodZastepczy.KwotaMarzyKorekta),
                    KosztPlusMarza = Math.Round(subStawkaResult.KosztSamochodZastepczy.KosztPlusMarza),
                    KosztPlusMarzaKorekta = Math.Round(subStawkaResult.KosztSamochodZastepczy.KosztPlusMarzaKorekta)
                },

                KosztSerwis = new Koszt
                {
                    RozkladMarzy = subStawkaResult.KosztSerwis.RozkladMarzy,
                    RozkladMarzyKorekta = subStawkaResult.KosztSerwis.RozkladMarzyKorekta,
                    KwotaMarzy = Math.Round(subStawkaResult.KosztSerwis.KwotaMarzy),
                    KwotaMarzyKorekta = Math.Round(subStawkaResult.KosztSerwis.KwotaMarzyKorekta),
                    KosztPlusMarza = Math.Round(subStawkaResult.KosztSerwis.KosztPlusMarza),
                    KosztPlusMarzaKorekta = Math.Round(subStawkaResult.KosztSerwis.KosztPlusMarzaKorekta)
                },

                KosztAdmin = new Koszt
                {
                    RozkladMarzy = subStawkaResult.KosztAdmin.RozkladMarzy,
                    RozkladMarzyKorekta = subStawkaResult.KosztAdmin.RozkladMarzyKorekta,
                    KwotaMarzy = Math.Round(subStawkaResult.KosztAdmin.KwotaMarzy),
                    KwotaMarzyKorekta = Math.Round(subStawkaResult.KosztAdmin.KwotaMarzyKorekta),
                    KosztPlusMarza = Math.Round(subStawkaResult.KosztAdmin.KosztPlusMarza),
                    KosztPlusMarzaKorekta = Math.Round(subStawkaResult.KosztAdmin.KosztPlusMarzaKorekta)
                }
            };

            if (_tabele.IsReport)
            {
                adjustReportToExpressDemands(result, _kalkulacja);
                string reportAsHtml = GetReportAsHtml();
                result.ReportHtml = reportAsHtml;
            }

            return result;

        }

        public string GetReportAsHtml()
        {
            if (null == _tabele) return string.Empty;
            if (null == _tabele.Report) return string.Empty;


            string reportAsHtml = CalcReportHtmlBuilder.Instance.BuildHTML(_tabele.Report);
            return reportAsHtml;
        }

        #endregion

        #region <private methods>

        private void AddInputDataToReport(Kalkulacja kalkulacja)
        {
            try
            {
                string tableName = "GŁÓWNE PARAMETRY " + '(' + kalkulacja.Id + ')';
                CalcReportNameValueTable table = new CalcReportNameValueTable(tableName);
                table.UniqueName = CalcReportNameValueTable.UNIQUE_NAME_DANE;
                _tabele.Report.AddItem(table);


                table.AddValue("Data utworzenia", DateTime.Now.ToString());
                table.AddValue("OkresUzytkowania", kalkulacja.OkresUzytkowania);
                table.AddValue("Przebieg", kalkulacja.Przebieg);
                table.AddValue("Produkt", (null != kalkulacja.LiniaProduktowa) ? kalkulacja.LiniaProduktowa.NazwaLiniiProduktowej : "?");
                table.AddValue("Rocznik", kalkulacja.Rocznik);
                table.AddValue("Marka", getMarkaInfo(kalkulacja));
                table.AddValue("Model", getModelInfo(kalkulacja));
                table.AddValue("Symbol E-tax", getSymbolEtaxInfo(kalkulacja));
                table.AddValue("Nadwozie", (null != kalkulacja.Model) ? kalkulacja.RodzajNadwozia : "?");
                table.AddValue("Marża", kalkulacja.Marza);
                table.AddValue("CzynszInicjalny", kalkulacja.CzynszInicjalny);
                table.AddValue("CenaCennikowa", kalkulacja.CenaCennikowa);
                table.AddValue("Opcje fabryczne (suma)", kalkulacja.OpcjeFabryczne.Sum(o => Kwota.GetAsDecimal(o.CenaCennikowa)));
                table.AddValue("CzyMetalik", kalkulacja.Metalik.GetValueOrDefault());
                table.AddValue("LakierMetalik", kalkulacja.LakierMetalik);
                table.AddValue("Rabat %", kalkulacja.RabatProcent);
                table.AddValue("Rabat kwotowo", kalkulacja.RabatKwotowo);
                table.AddValue("Homologacja", kalkulacja.Homologacja);
                table.AddValue("Opcje z WR (suma)", kalkulacja.OpcjaSerwisowas.Sum(o => Kwota.GetAsDecimal(o.CenaCennikowa)));
                table.AddValue("PakietSerwisowy", kalkulacja.PakietSerwisowy);
                table.AddValue("InneKosztySerwisowaniaKwota", kalkulacja.InneKosztySerwisowaniaKwota);
                table.AddValue("KorektaWR", kalkulacja.KorektaWR);
                table.AddValue("ZOponami", kalkulacja.ZOponami);
                table.AddValue("Rozmiar opon", (null != kalkulacja.RozmiarOpon) ? kalkulacja.RozmiarOpon.Srednica : "?");
                table.AddValue("KlasaOpon", kalkulacja.KlasaOpon);
                table.AddValue("LiczbaKompletowOpon", kalkulacja.LiczbaKompletowOpon);
                table.AddValue("SamochodZastepczy", kalkulacja.SamochodZastepczy);
                table.AddValue("NaukaJazdy", kalkulacja.NaukaJazdy);
                table.AddValue("ExpressPlaciUbezpieczenie", kalkulacja.ExpressPlaciUbezpieczenie);
                table.AddValue("DoubezpieczenieKradziezy", kalkulacja.DoubezpieczenieKradziezy);
                table.AddValue("CzyDemontazKraty", kalkulacja.isDemontaz.GetValueOrDefault());
                table.AddValue("CzyHak", kalkulacja.Hak.GetValueOrDefault());
                table.AddValue("CzyGPS", kalkulacja.CzyGPS.GetValueOrDefault());
            }
            catch (Exception exc)
            {
                logger.Error(exc, "AddInputDataToReport");
            }
        }

        string getModelInfo(Kalkulacja kalkulacja)
        {
            if (null == kalkulacja.Model) return "?";

            string nazwa = kalkulacja.Model.Wersja;
            int id = kalkulacja.Model.Id;

            return nazwa + '(' + id + ')';
        }

        string getMarkaInfo(Kalkulacja kalkulacja)
        {

            if (null == kalkulacja.Model) return "?";
            if (null == kalkulacja.Model.Marka) return "?";

            string nazwa = kalkulacja.Model.Marka.Nazwa;
            int id = kalkulacja.Model.Marka.Id;

            return nazwa + '(' + id + ')';

        }

        string getSymbolEtaxInfo(Kalkulacja kalkulacja)
        {
            if (null == kalkulacja.Model) return "?";
            if (null == kalkulacja.Model.SymbolEuroTax) return "?";


            string nazwa = kalkulacja.Model.SymbolEuroTax.Symbol;
            int id = kalkulacja.Model.SymbolEuroTax.Id;

            return nazwa + '(' + id + ')';
        }

        private void adjustReportToExpressDemands(Result result, Kalkulacja kalkulacja)
        {

            // dostosowanie diagnostyki do zyczen B.Guzik

            CalcReportNameValueTable table = new CalcReportNameValueTable("WYNIK NETTO");
            table.UniqueName = CalcReportNameValueTable.UNIQUE_NAME_MAIN_RESULTS;
            table.AddValue("Cena zakupu", result.CenaZakupu);
            table.AddValue("Serwis", result.LacznieKosztySerwisowe);
            table.AddValue("Opony", result.LacznyKosztOpon);
            table.AddValue("Ubezpieczenie", result.LacznieUbezpieczenie);
            table.AddValue("Koszty dodatkowe", result.KosztyDodatkowe);
            table.AddValue("Samochód zastępczy", result.LacznieSamochodZastepczy);
            table.AddValue("Finansowe", result.LacznyKosztCzesciOdsetkowejRaty);
            table.AddValue("Utrata wartości", result.UtrataWartosci);
            table.AddValue("Koszt dzienny", result.KosztDzienny);
            table.AddValue("Marza na kontrakcie", result.MarzaNaKontrakcie);
            table.AddValue("Oferowana stawka", result.LacznaStawka);
            table.AddValue("Przychód", result.Przychod);
            _tabele.Report.AddItem(table);


            bool isOk = _tabele.Report.MoveItemAfterItem(CalcReportItem.UNIQUE_NAME_MAIN_RESULTS, CalcReportItem.UNIQUE_NAME_DANE);
            if (isOk)
                isOk = _tabele.Report.MoveItemAfterItem(CalcReportItem.UNIQUE_NAME_TABELA_EUROTAX, CalcReportItem.UNIQUE_NAME_MAIN_RESULTS);
            if (isOk)
                isOk = _tabele.Report.MoveItemAfterItem(CalcReportItem.UNIQUE_NAME_TABELA_SERWISOWA, CalcReportItem.UNIQUE_NAME_TABELA_EUROTAX);

        }

        private int GetPrzebiegForLiniaProduktowa(int przebieg)
        {
            int result = przebieg;
            string liniaProduktowa = _kalkulacja.LiniaProduktowa != null ? _kalkulacja.LiniaProduktowa.NazwaLiniiProduktowej : String.Empty;
            if (liniaProduktowa == MagazynyConst.LTR)
                result += _tabele.Parametry.KorektaPrzebieguDlaLTR;

            return result;
        }
        #endregion
    }
}
