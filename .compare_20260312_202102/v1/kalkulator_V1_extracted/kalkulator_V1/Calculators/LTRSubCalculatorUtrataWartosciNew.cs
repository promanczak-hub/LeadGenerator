using Express.BaseTypes;
using Express.Logic.Kalkulatory.LTR.Cache;
using Express.Logic.Kalkulatory.LTR.CalculatorReport;
using Express.Logic.Kalkulatory.LTR.CalculatorTypes;
using Express.WD;
using System;
using System.Linq;
using TF.BusinessFramework.Patterns.UnitOfWork;

namespace Express.Logic.Kalkulatory.LTR.Calculators
{
    internal class LTRSubCalculatorUtrataWartosciNew : LTRSubCalculatorNew<LTRSubCalculatorUtrataWartosciNewInput, LTRSubCalculatorUtrataWartosciNewOutput>
    {
        public override LTRSubCalculatorUtrataWartosciNewOutput Calculate(Kalkulacja kalkulacja, LTRSubCalculatorUtrataWartosciNewInput input, CalcReport report, IUnitOfWork unitOfWork)
        {
            LTRParametry ltrParametry = LTRAdminParametryCache.GetLTRParametry(unitOfWork);

            decimal cenaKatalogowaBrutto = kalkulacja.CenaCennikowa.Wartosc;
            decimal cenaKatalogowaBruttoPoRabacie = kalkulacja.CenaCennikowa.Wartosc * (1.0m - kalkulacja.RabatProcent);

            decimal lacznaCenaDoposazeniaBrutto = GetLacznaCenaDoposazeniaBrutto(kalkulacja, includeRabat: false);         
            decimal lacznaCenaDoposazeniaBruttoPoRabacie = GetLacznaCenaDoposazeniaBrutto(kalkulacja, includeRabat: true);

            decimal lacznaCenaZakupuBrutto = cenaKatalogowaBrutto + lacznaCenaDoposazeniaBrutto;
            decimal lacznaCenaZakupuBruttoPoRabacie = cenaKatalogowaBruttoPoRabacie + lacznaCenaDoposazeniaBruttoPoRabacie;

            decimal lacznaCenaZakupuNetto = lacznaCenaZakupuBrutto / ltrParametry.StawkaVAT;
            decimal lacznaCenaZakupuNettoPoRabacie = lacznaCenaZakupuBruttoPoRabacie / ltrParametry.StawkaVAT;

            decimal okresPlusDoposazenie = GetOkresPlusDoposazenie(kalkulacja, input, cenaKatalogowaBrutto, lacznaCenaDoposazeniaBrutto, ltrParametry, unitOfWork);

            string kolor = kalkulacja.Metalik == true ? "metalik" : "niemetalik";
            decimal korektaKolorProcent = LTRAdminParamsWRCache.LTRAdminKorektaWRKolorDictionary.Get(kolor, () => LTRAdminParamsWRCache.Kolor2LTRAdminKorektaWRKolor(kolor, unitOfWork));

            decimal korektaZabudowaProcent = LTRAdminParamsWRCache.LTRAdminKorektaWRZabudowaDictionary.Get(kalkulacja.RodzajZabudowy ?? string.Empty, () => LTRAdminParamsWRCache.RodzajZabudowy2LTRAdminKorektaWRZabudowa(kalkulacja.RodzajZabudowy, unitOfWork));

            decimal korektaPrzebiegu = GetKorektaPrzebiegu(kalkulacja, input, okresPlusDoposazenie, unitOfWork);

            //korekta ręczna o wartość z wejścia nie liczoną - jak bez tego to 0 ma być
            decimal korektaWR = kalkulacja.KorektaWR;

            decimal WRBrutto = okresPlusDoposazenie
                             + (cenaKatalogowaBrutto * korektaKolorProcent)
                             + (lacznaCenaZakupuBrutto * korektaZabudowaProcent)
                             - korektaPrzebiegu
                             + korektaWR;

            decimal korektaRocznikProcent = LTRAdminParamsWRCache.LTRAdminKorektaWRRocznikDictionary.Get(kalkulacja.Rocznik, () => LTRAdminParamsWRCache.Rocznik2LTRAdminKorektaWRRocznik(kalkulacja.Rocznik, unitOfWork));
            decimal WRBruttoPoKorekcieZaRocznik = WRBrutto * (1.0m + korektaRocznikProcent);
            decimal WRdlaLOBrutto = WRBruttoPoKorekcieZaRocznik * (1.0m + ltrParametry.PrzewidywanaCenaSprzedazyLO);
            
            decimal utrataWartosciBezCzynszuBrutto = Math.Max(lacznaCenaZakupuBruttoPoRabacie - WRBruttoPoKorekcieZaRocznik, 0.0m);

            decimal czynszInicjalnyBrutto = Kwota.GetAsDecimal(kalkulacja.CzynszInicjalny);
            decimal utrataWartosciZCzynszemBrutto = Math.Max(utrataWartosciBezCzynszuBrutto - czynszInicjalnyBrutto, 0.0m);

            LTRSubCalculatorUtrataWartosciNewOutput output = new LTRSubCalculatorUtrataWartosciNewOutput
            {
                WR = WRBruttoPoKorekcieZaRocznik / ltrParametry.StawkaVAT,                
                WRdlaLO = WRdlaLOBrutto / ltrParametry.StawkaVAT,
                UtrataWartosciBEZczynszu = utrataWartosciBezCzynszuBrutto / ltrParametry.StawkaVAT,
                UtrataWartosciZCzynszemInicjalnym = utrataWartosciZCzynszemBrutto / ltrParametry.StawkaVAT,
                KorektaZaPrzebiegKwotowo = korektaPrzebiegu / ltrParametry.StawkaVAT,
                KorektaAdministracyjnaKwotowo = 0.0m,
            };

            if (input.IsReport)
                AppendDataToReport(report, output, lacznaCenaZakupuNetto, lacznaCenaZakupuNettoPoRabacie);

            return output;
        }        

        private decimal GetOkresPlusDoposazenie(Kalkulacja kalkulacja, LTRSubCalculatorUtrataWartosciNewInput input, decimal cenaKatalogowaBrutto, decimal lacznaCenaDoposazeniaBrutto, LTRParametry ltrParametry, IUnitOfWork unitOfWork)
        {
            Fuel rodzajPaliwa = Kalkulacja2Fuel(kalkulacja);
            int liczbaLat = CalculateWiekSamochodu(input.OkresUzytkowania, ltrParametry.CzasPrzygotowaniaDoSprzedazy);
            int klasaWRId = Kalkulacja2KlasaWRId(kalkulacja);
            int markaId = kalkulacja.Model.MarkaId;

            decimal wrKlasaProcent = LTRAdminParamsWRCache.LTRAdminTabelaWRKlasaDictionary.Get(LTRAdminParamsWRCache.LTRAdminTabelaWRKlasaDescriptor(rodzajPaliwa, klasaWRId), () => LTRAdminParamsWRCache.GetLTRAdminTabelaWRKlasa(rodzajPaliwa, klasaWRId, unitOfWork));
            decimal wrMarkaProcent = LTRAdminParamsWRCache.LTRAdminKorektaWRMarkaDictionary.Get(LTRAdminParamsWRCache.LTRAdminKorektaWRMarkaDescriptor(rodzajPaliwa, klasaWRId, markaId), () => LTRAdminParamsWRCache.GetLTRAdminKorektaWRMarka(rodzajPaliwa, klasaWRId, markaId, unitOfWork));
           
            decimal wrProcent48 = wrKlasaProcent + wrMarkaProcent;
            decimal wrWartosc48 = wrProcent48 * cenaKatalogowaBrutto;//BS3

            decimal WRWartoscPoLatach = wrWartosc48;//BO3
            if (liczbaLat < 4)
            {
                for (int rok = 3; rok >= liczbaLat; rok--)
                {
                    decimal deprecjacjaZaRokProcent = LTRAdminParamsWRCache.LTRAdminTabelaDeprecjacjaOkresDictionary.Get(LTRAdminParamsWRCache.LTRAdminTabelaDeprecjacjaOkresDescriptor(rodzajPaliwa, klasaWRId, rok), () => LTRAdminParamsWRCache.GetLTRAdminTabelaDeprecjacjaOkres(rodzajPaliwa, klasaWRId, rok, unitOfWork));
                    WRWartoscPoLatach = WRWartoscPoLatach * (1.0m + deprecjacjaZaRokProcent);
                }
            }
            else if (liczbaLat > 4)
            {
                for (int rok = 5; rok <= liczbaLat; rok++)
                {
                    decimal deprecjacjaZaRokProcent = LTRAdminParamsWRCache.LTRAdminTabelaDeprecjacjaOkresDictionary.Get(LTRAdminParamsWRCache.LTRAdminTabelaDeprecjacjaOkresDescriptor(rodzajPaliwa, klasaWRId, rok), () => LTRAdminParamsWRCache.GetLTRAdminTabelaDeprecjacjaOkres(rodzajPaliwa, klasaWRId, rok, unitOfWork));
                    WRWartoscPoLatach = WRWartoscPoLatach *  (1.0m - deprecjacjaZaRokProcent);
                }
            }


            
            decimal doposazenieProcent = LTRAdminParamsWRCache.LTRAdminTabelaWRDoposazenieDictionary.Get(LTRAdminParamsWRCache.LTRAdminTabelaWRDoposazenieDescriptor(liczbaLat, rodzajPaliwa, klasaWRId), () => LTRAdminParamsWRCache.GetLTRAdminTabelaWRDoposazenie(liczbaLat, rodzajPaliwa, klasaWRId, unitOfWork));//BQ3

            return WRWartoscPoLatach + lacznaCenaDoposazeniaBrutto * doposazenieProcent;
        }

        private int CalculateWiekSamochodu(int okresUzytkowania, int czasPrzygotowaniaDoSprzedazy)
        {
            DateTime dataKalkulacji = DateTime.Now;
            DateTime dataZakonczeniaKontraktu = dataKalkulacji.AddMonths(okresUzytkowania).AddMonths(czasPrzygotowaniaDoSprzedazy);

            return dataZakonczeniaKontraktu.Year - dataKalkulacji.Year;
        }

        private decimal GetLacznaCenaDoposazeniaBrutto(Kalkulacja kalkulacja, bool includeRabat)
        {
            decimal rabatProcent = includeRabat ? kalkulacja.RabatProcent : 0.0m;

            decimal opcjeFabryczneSuma = kalkulacja.OpcjeFabryczne.Select(o => Kwota.GetAsDecimal(o.CenaCennikowa) * (o.isNierabatowany == true ? 1.0m : 1.0m - rabatProcent)).DefaultIfEmpty().Sum();

            return opcjeFabryczneSuma;
        }

        private decimal GetKorektaPrzebiegu(Kalkulacja kalkulacja, LTRSubCalculatorUtrataWartosciNewInput input, decimal okresPlusDoposazenie, IUnitOfWork unitOfWork)
        {
            int klasaWRId = Kalkulacja2KlasaWRId(kalkulacja);

            decimal przebiegPonizej190 = Math.Min(input.Przebieg, 190000.0m) - 140000.0m;//AD3
            decimal przebiegPowyzej190 = Math.Max(input.Przebieg - 190000.0m, 0m);//AF3

            var korektaPrzebieg = LTRAdminParamsWRCache.LTRAdminTabelaWRPrzebiegDictionary.Get(klasaWRId, () => LTRAdminParamsWRCache.KlasaWRId2LTRAdminTabelaWRPrzebiegDTO(klasaWRId, unitOfWork));
            decimal korektaProcentPonizej190 = korektaPrzebieg != null ? korektaPrzebieg.KorektaProcentPonizej190 : 0.0m;
            decimal korektaProcentPowyzej190 = korektaPrzebieg != null ? korektaPrzebieg.KorektaProcentPowyzej190 : 0.0m;

            return korektaProcentPonizej190 * okresPlusDoposazenie * (przebiegPonizej190 / 10000.0m)
                 + korektaProcentPowyzej190 * okresPlusDoposazenie * (przebiegPowyzej190 / 10000.0m);
        }

        private void AppendDataToReport(CalcReport report, LTRSubCalculatorUtrataWartosciNewOutput output, decimal lacznaCenaZakupuNetto, decimal lacznaCenaZakupuNettoPoRabacie)
        {
            CalcReportNameValueTable table = new CalcReportNameValueTable("UTRATA WARTOŚCI (NOWA)");
            report.AddItem(table);

            decimal wrProcent = lacznaCenaZakupuNetto > 0 ? output.WR / lacznaCenaZakupuNetto : 0;


            table.AddValue("Łączna cena zakupu (netto)", lacznaCenaZakupuNetto);
            table.AddValue("Łączna cena zakupu po rabacie (netto)", lacznaCenaZakupuNettoPoRabacie);
            table.AddValue("WR (netto)", output.WR);
            table.AddValue("WR %", wrProcent * 100.0m);
            table.AddValue("WR dla LO (netto)", output.WRdlaLO);
            table.AddValue("Utrata wartości (netto)", output.UtrataWartosciBEZczynszu, isBold: true);
            table.AddValue("Utrata wartości z czynszem inicjalnym (netto)", output.UtrataWartosciZCzynszemInicjalnym);
            table.AddValue("KorektaZaPrzebiegKwotowo (netto)", output.KorektaZaPrzebiegKwotowo);
            table.AddValue("KorektaAdministracyjnaKwotowo (netto)", output.KorektaAdministracyjnaKwotowo);
        }
    }

}
