using Express.BaseTypes;
using Express.Logic.Kalkulatory.LTR.Cache;
using Express.Logic.Kalkulatory.LTR.CalculatorReport;
using Express.Logic.Kalkulatory.LTR.CalculatorTypes;
using Express.Types.Magazyny;
using Express.WD;
using System;
using System.Collections.Generic;
using System.Linq;
using TF.BusinessFramework.Patterns.UnitOfWork;

namespace Express.Logic.Kalkulatory.LTR.Calculators
{
    internal class LTRSubCalculatorSerwisNew : LTRSubCalculatorNew<LTRSubCalculatorSerwisNewInput, LTRSubCalculatorSerwisNewOutput> 
    {
        public override LTRSubCalculatorSerwisNewOutput Calculate(Kalkulacja kalkulacja, LTRSubCalculatorSerwisNewInput input, CalcReport report, IUnitOfWork unitOfWork)
        {
            LTRParametry ltrParametry = LTRAdminParametryCache.GetLTRParametry(unitOfWork);

            int przebiegSkorygowany = input.Przebieg;
            if (kalkulacja.LiniaProduktowaId == LiniaProduktowaConst.LTRId)
                przebiegSkorygowany += ltrParametry.KorektaPrzebieguDlaLTR;

            decimal kosztPrzegladuPodstawowego = GetKosztPrzegladuPodstawowego(kalkulacja.Model);

            List<TabelaSerwisowa> pozycjeTabeliSerwisowej = GetPozycjeTabeliSerwisowej(kalkulacja, input, unitOfWork);

            decimal sumaKosztowZPrzebiegu = CalculateKosztySerwisu(kalkulacja, przebiegSkorygowany, unitOfWork);

            int wynajemWLatach = (int)Math.Floor(input.OkresUzytkowania / 12.0);

            int iloscPrzegladowZPrzebiegu = CalculateIloscPrzegladow(pozycjeTabeliSerwisowej, input.PrzebiegPoczatkowy, input.PrzebiegPoczatkowy + przebiegSkorygowany);

            decimal inneKosztySerwisowaniaNetto = kalkulacja.InneKosztySerwisowaniaKwota.Wartosc / ltrParametry.StawkaVAT;

            decimal kosztyLacznieZPrzebiegiemPodstawowym = GetKosztyLacznieZPrzebiegiemPodstawowym(kalkulacja, iloscPrzegladowZPrzebiegu, wynajemWLatach, sumaKosztowZPrzebiegu, inneKosztySerwisowaniaNetto, kosztPrzegladuPodstawowego);

            decimal korektaAdminProcent = 0.0m;
            if (kalkulacja.LiniaProduktowaId == LiniaProduktowaConst.LTRId)
                korektaAdminProcent = kosztyLacznieZPrzebiegiemPodstawowym * ltrParametry.KorektaSerwisProcent;

            decimal pakietSerwisowyNetto = kalkulacja.PakietSerwisowy.Wartosc / ltrParametry.StawkaVAT;

            decimal pakietSerwisowyKorektaKosztow = pakietSerwisowyNetto + inneKosztySerwisowaniaNetto;

            decimal kosztyLacznie = GetKosztyLacznie(kalkulacja, iloscPrzegladowZPrzebiegu, wynajemWLatach, pakietSerwisowyNetto, pakietSerwisowyKorektaKosztow, sumaKosztowZPrzebiegu, inneKosztySerwisowaniaNetto, korektaAdminProcent, kosztPrzegladuPodstawowego);

            if (input.IsReport)
                AppendDataToReport(report, przebiegSkorygowany, kosztPrzegladuPodstawowego, sumaKosztowZPrzebiegu, wynajemWLatach, iloscPrzegladowZPrzebiegu, inneKosztySerwisowaniaNetto, kosztyLacznieZPrzebiegiemPodstawowym, korektaAdminProcent, pakietSerwisowyKorektaKosztow, kosztyLacznie);

            LTRSubCalculatorSerwisNewOutput output = new LTRSubCalculatorSerwisNewOutput()
            {
                SerwisNetto = kosztyLacznie
            };

            return output;
        }        

        private decimal CalculateKosztySerwisu(Kalkulacja kalkulacja, decimal przebiegSamochodu, IUnitOfWork unitOfWork)
        {
            int klasaId = Kalkulacja2KlasaId(kalkulacja);
            int markaId = kalkulacja.Model.MarkaId;
            Fuel rodzajPaliwa = Kalkulacja2Fuel(kalkulacja);
            string rodzajKosztow = kalkulacja.RodzajKosztow ?? string.Empty;

            decimal stawkaZaKM = LTRAdminParamsSerwisCache.LTRAdminKosztySerwisoweDictionary.Get(LTRAdminParamsSerwisCache.LTRAdminKosztySerwisoweDescriptor(klasaId, markaId, rodzajPaliwa, rodzajKosztow), () => LTRAdminParamsSerwisCache.GetLTRAdminKosztySerwisowe(klasaId, markaId, rodzajPaliwa, rodzajKosztow, unitOfWork));

            return przebiegSamochodu * stawkaZaKM;
        }

        private List<TabelaSerwisowa> GetPozycjeTabeliSerwisowej(Kalkulacja kalkulacja, LTRSubCalculatorSerwisNewInput input, IUnitOfWork unitOfWork)
        {
            List<TabelaSerwisowa> pozycjeTabeli = new List<TabelaSerwisowa>();

            if (kalkulacja.Model.MarkaId == 20 && input.PrzebiegPoczatkowy > 0)
                pozycjeTabeli = Enumerable.Range(1, 25).Select(i => new TabelaSerwisowa { Przebieg = i * 10000, ModelId = kalkulacja.Model.Id, KwotaSerwisu = 1000.0m, KwotaNonASO = 1000.0m }).ToList();
            else
                pozycjeTabeli = unitOfWork.GetRepository<TabelaSerwisowa>().AsQueryable().Where(t => t.ModelId == kalkulacja.Model.Id).OrderBy(p => p.Przebieg).ToList();            

            return pozycjeTabeli;
        }

        private int CalculateIloscPrzegladow(List<TabelaSerwisowa> pozycjeTabeliSerwisowej, int przebiegPoczatkowy, int przebiegKoncowy)
        {
            return pozycjeTabeliSerwisowej.Where(pt => pt.Przebieg >= przebiegPoczatkowy && pt.Przebieg <= przebiegKoncowy).Count();
        }

        private decimal GetKosztyLacznieZPrzebiegiemPodstawowym(Kalkulacja kalkulacja, int iloscPrzegladowZPrzebiegu, int wynajemWLatach, decimal sumaKosztowZPrzebiegu, decimal inneKosztySerwisowaniaNetto, decimal kosztPrzegladuPodstawowego)
        {
            decimal result = sumaKosztowZPrzebiegu;            
            if (iloscPrzegladowZPrzebiegu < wynajemWLatach)
                result += (wynajemWLatach - iloscPrzegladowZPrzebiegu) * kosztPrzegladuPodstawowego;
                        
            result = result + inneKosztySerwisowaniaNetto;

            return result;
        }

        private decimal GetKosztyLacznie(Kalkulacja kalkulacja, int iloscPrzegladowZPrzebiegu, int wynajemWLatach, decimal pakietSerwisowyNetto, decimal pakietSerwisowyKorektaKosztow, decimal sumaKosztowZPrzebiegu, decimal inneKosztySerwisowaniaNetto, decimal korektaAdminProcent, decimal kosztPrzegladuPodstawowego)
        {
            decimal kosztyLacznie = 0.0m;

            if (kalkulacja.CzyUwzgledniaSerwisowanie ?? false)
            {
                if (pakietSerwisowyNetto > 0)
                {
                    kosztyLacznie = pakietSerwisowyKorektaKosztow;
                }
                else
                {
                    kosztyLacznie = sumaKosztowZPrzebiegu;
                    if (iloscPrzegladowZPrzebiegu < wynajemWLatach)
                        kosztyLacznie += (wynajemWLatach - iloscPrzegladowZPrzebiegu) * kosztPrzegladuPodstawowego;

                    kosztyLacznie = kosztyLacznie + inneKosztySerwisowaniaNetto - korektaAdminProcent;
                }
            }

            return kosztyLacznie;
        }

        private static void AppendDataToReport(CalcReport report, decimal przebiegSkorygowany, decimal kosztPrzegladuPodstawowego, decimal sumaKosztowZPrzebiegu, int wynajemWLatach, int iloscPrzegladowZPrzebiegu, decimal inneKosztySerwisowaniaNetto, decimal kosztyLacznieZPrzebiegiemPodstawowym, decimal korektaAdminProcent, decimal pakietSerwisowyKorektaKosztow, decimal kosztyLacznie)
        {
            CalcReportNameValueTable table = new CalcReportNameValueTable("SERWIS (NOWY)");
            report.AddItem(table);

            table.AddValue("PRZEBIEG SKORYGOWANY", przebiegSkorygowany);
            table.AddValue("SUMA KOSZTÓW Z PRZEBIEGU", sumaKosztowZPrzebiegu);
            table.AddValue("WYNAJEM W LATACH", wynajemWLatach);
            table.AddValue("ILOŚĆ PRZEGLĄDÓW Z PRZEBIEGU", iloscPrzegladowZPrzebiegu);
            table.AddValue("INNE KOSZTY SERWISOWANIA", inneKosztySerwisowaniaNetto);
            table.AddValue("KOSZTY ŁĄCZNIE Z PRZEBIEGIEM PODSTAWOWYM", kosztyLacznieZPrzebiegiemPodstawowym);
            table.AddValue("KOREKTA ADMIN %", korektaAdminProcent);
            table.AddValue("PAKIET SERWISOWY + KOREKTA KOSZTÓW", pakietSerwisowyKorektaKosztow);
            table.AddValue("KOSZTY ŁĄCZNIE", kosztyLacznie, true);
            table.AddValue("KOSZT PRZEGLĄDU PODSTAWOWEGO", kosztPrzegladuPodstawowego);
        }

        private decimal GetKosztPrzegladuPodstawowego(Model model)
        {

            decimal? koszt = null;

            if (null != model)
            {
                if (null != model.Klasa &&
                    (!Kwota.IsNullOrZero(model.Klasa.KosztPrzegladuPodstawowego)))
                {
                    koszt = Kwota.GetAsDecimal(model.Klasa.KosztPrzegladuPodstawowego);
                }
                else if (!Kwota.IsNullOrZero(model.KosztPrzegladuTechnicznego))
                {
                    koszt = Kwota.GetAsDecimal(model.KosztPrzegladuTechnicznego);
                }
            }
            else
            {
                throw new Exception("Brak modelu w kalkulacji");
            }

            if (!koszt.HasValue || koszt.Value == 0)
            {
                string message = string.Format("Brak kosztu przegladu podstawowego (technicznego) dla modelu id={0}", model.Id);
                throw new Exception(message);
            }

            return koszt.Value;

        }
    }

}
