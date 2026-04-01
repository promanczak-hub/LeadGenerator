using Express.Logic.Kalkulatory.LTR.CalculatorReport;
using Express.Logic.Kalkulatory.LTR.CalculatorTypes;
using Express.Types.Modele;
using Express.WD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Express.Logic.Kalkulatory.LTR
{

    public class LTRSubCalculatorSerwis : LTRSubCalculator
    {
        public class Data : LTRSubCalculatorData
        {
            public int PrzebiegSkorygowany { get; set; }

        }

        #region constructor
        public LTRSubCalculatorSerwis(Kalkulacja kalkulacja, LTRTabele tabele) : base(kalkulacja, tabele)
        {
            
        }
        #endregion
       


        public LTRSubCalculatorSerwisNewOutput Policz(Data data)
        {
            // Obliczenia do przebiegu, bierzemy przebieg skorygowany o wartość administracyjną KorektaPrzebieguDlaLTR
            Przebieg sumaPrzebiegow = GetPrzebiegSuma(data.PrzebiegSkorygowany, data.StarySamochod != null ? data.StarySamochod.PrzebiegPoczatkowy : (int?)null);
            validDataAndThrowExceptionIfNotValid(sumaPrzebiegow);

            //Wynajem w latach
            int wynajemWLatach = (int)Math.Floor(_tabele.Okres / 12.0);

            // Ilość przeglądów
            int iloscPrzegladowZPrzebiegu = sumaPrzebiegow.IloscPrzegladow;

            // Inne koszty serwisowania
            decimal inneKosztySerwisowaniaNetto = Kwota.GetAsDecimal(_kalkulacja.InneKosztySerwisowaniaKwota) / _tabele.Parametry.StawkaVAT;

            decimal sumaZKosztowPrzebiegu = sumaPrzebiegow.ObliczeniaNetto;

            // Koszty łączne z przebiegiem podstawowym
            decimal kosztyLaczneZPrzebiegiemPodstawowym = GetKosztyLaczneZPrzebiegiemPodstawowym(iloscPrzegladowZPrzebiegu, wynajemWLatach, sumaZKosztowPrzebiegu, inneKosztySerwisowaniaNetto);

            string liniaProduktowa = _kalkulacja.LiniaProduktowa != null ? _kalkulacja.LiniaProduktowa.NazwaLiniiProduktowej : String.Empty;

            // korekta Admin %
            decimal korektaAdminProcent = (liniaProduktowa == "LTR") ? kosztyLaczneZPrzebiegiemPodstawowym * _tabele.Parametry.KorektaSerwisProcent : 0;

            decimal pakietSerwisowy = Kwota.GetAsDecimal(_kalkulacja.PakietSerwisowy);
            decimal pakietSerwisowyNetto = pakietSerwisowy / _tabele.Parametry.StawkaVAT;

            // Pakiet Serwisowy Korekta Kosztow
            decimal pakietSerwisowyKorektaKosztow = pakietSerwisowyNetto + inneKosztySerwisowaniaNetto;

            // Koszty łącznie - zmiana
            decimal kosztyLacznie = GetKosztyLacznie(iloscPrzegladowZPrzebiegu, wynajemWLatach, pakietSerwisowyNetto, pakietSerwisowyKorektaKosztow, sumaZKosztowPrzebiegu, inneKosztySerwisowaniaNetto, korektaAdminProcent);

            if (_tabele.IsReport)
            {
                CalcReportNameValueTable table = new CalcReportNameValueTable("SERWIS");
                _tabele.Report.AddItem(table);

                table.AddValue("sumaZKosztowPrzebiegu", sumaZKosztowPrzebiegu);
                table.AddValue("wynajemWLatach", wynajemWLatach);
                table.AddValue("iloscPrzegladowZPrzebiegu", iloscPrzegladowZPrzebiegu);
                table.AddValue("inneKosztySerwisowaniaNetto", inneKosztySerwisowaniaNetto);
                table.AddValue("kosztyLaczneZPrzebiegiemPodstawowym", kosztyLaczneZPrzebiegiemPodstawowym);
                table.AddValue("korektaAdminProcent", korektaAdminProcent);
                table.AddValue("pakietSerwisowyKorektaKosztow", pakietSerwisowyKorektaKosztow);
                table.AddValue("kosztyLacznie", kosztyLacznie, true);

                decimal KOSZT_PRZEGLADU_PODSTAWOWEGO = (iloscPrzegladowZPrzebiegu < wynajemWLatach) ? GetKosztPrzegladuPodstawowego(_kalkulacja.Model) : 0.0M;

                if (KOSZT_PRZEGLADU_PODSTAWOWEGO > 0)
                    table.AddValue("kosztPrzegladuPodstawowego", KOSZT_PRZEGLADU_PODSTAWOWEGO);
            }

            return new LTRSubCalculatorSerwisNewOutput
            {
                SerwisNetto = kosztyLacznie
            };
        }

        #region internal classes
        class Przebieg
        {
            public decimal ObliczeniaNetto { get; set; }
            public int IloscPrzegladow { get; set; }
        }
        #endregion

        #region internal helper methods

        void validDataAndThrowExceptionIfNotValid(Przebieg przebieg)
        {
            bool isValid = przebieg.ObliczeniaNetto > 0 || Kwota.GetAsDecimal(_kalkulacja.PakietSerwisowy) > 0;

            if (!isValid)
                throw new Exception("Koszty serwisowe = 0 oraz Pakiet Serwisowy = 0");
        }

        decimal GetKosztPrzegladuPodstawowego(Model model)
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

        decimal GetServiceAmount(TabelaSerwisowa serviceTable)
        {
            decimal serviceAmount = 0m;

            switch (_kalkulacja.RodzajKosztow)
            {
                case RodzajKosztowConsts.ASO:
                    serviceAmount = serviceTable.KwotaSerwisu ?? 0m;
                    break;
                case RodzajKosztowConsts.nonASO:
                    serviceAmount = serviceTable.KwotaNonASO ?? 0m;
                    break;
                default:
                    serviceAmount = serviceTable.KwotaSerwisu ?? 0m;
                    break;
            }

            return serviceAmount;
        }

        Przebieg GetPrzebiegSuma(int przebieg, int? przebiegStart)
        {
            List<TabelaSerwisowa> pozycjeTabeli = new List<TabelaSerwisowa>();
            Model model = _kalkulacja.Model;

            if (model != null)
            {
                if (model.MarkaId == 20 && przebiegStart.HasValue && przebiegStart.Value > 0)
                    pozycjeTabeli = Enumerable.Range(1, 25).Select(i => new TabelaSerwisowa { Przebieg = i * 10000, ModelId = model.Id, KwotaSerwisu = 1000.0m, KwotaNonASO = 1000.0m }).ToList();
                else
                    pozycjeTabeli = _tabele.iUnitOfWork.GetRepository<TabelaSerwisowa>().AsQueryable().Where(t => t.ModelId == model.Id).OrderBy(p => p.Przebieg).ToList();
            }

            if (pozycjeTabeli.Count == 0)
            {
                if (Kwota.IsNullOrZero(_kalkulacja.PakietSerwisowy))
                    throw new Exception("Brak pozycji w Tabeli Serwisowej");

                return new Przebieg
                {
                    IloscPrzegladow = 0,
                    ObliczeniaNetto = 0
                };
            }

           
            int przebiegPoczatkowy = przebiegStart ?? 0;
            int przebiegKoncowy = przebieg + przebiegPoczatkowy;

            decimal koszty = 0.0m;
            int przebiegOstatniej = 0;
            for (int i = 0; i < pozycjeTabeli.Count; i++)
            {
                TabelaSerwisowa pozycja = pozycjeTabeli[i];
                int przebiegTej = pozycja.Przebieg;
                decimal coeff = ((decimal)Overlap(przebiegPoczatkowy, przebiegKoncowy, przebiegOstatniej, przebiegTej)) / ((decimal)(przebiegTej - przebiegOstatniej));
                if(coeff > 0.0m)
                    koszty += coeff * GetServiceAmount(pozycja);
                przebiegOstatniej = przebiegTej;
            }

            int ilosc = pozycjeTabeli.Where(pt => pt.Przebieg >= przebiegPoczatkowy && pt.Przebieg <= przebiegKoncowy).Count();

            Przebieg suma = new Przebieg()
            {
                ObliczeniaNetto = koszty,
                IloscPrzegladow = ilosc
            };

            if (_tabele.IsReport)
            {
                CalcReportNameValueTable table = new CalcReportNameValueTable("Tabela serwisowa");
                table.UniqueName = CalcReportNameValueTable.UNIQUE_NAME_TABELA_SERWISOWA;
                table.AddValue("Model Id", _kalkulacja.ModelId);
                foreach (TabelaSerwisowa ts in pozycjeTabeli)
                    table.AddValue(ts.Przebieg.ToString(), GetServiceAmount(ts));
                _tabele.Report.AddItem(table);
            }

            return suma;
        }


        private static int Overlap(int s1, int e1, int s2, int e2)
        {
            return Math.Max(0, Math.Min(e1, e2) - Math.Max(s1, s2));
        }

        decimal GetKosztyLaczneZPrzebiegiemPodstawowym(int iloscPrzegladowZPrzebiegu, int wynajemWLatach, decimal sumaZKosztowPrzebiegu, decimal inneKosztySerwisowaniaNetto)
        {
            decimal koszt0 = 0;
            decimal KOSZT_PRZEGLADU_PODSTAWOWEGO = 0;
            if (iloscPrzegladowZPrzebiegu < wynajemWLatach)
            {
                KOSZT_PRZEGLADU_PODSTAWOWEGO = GetKosztPrzegladuPodstawowego(_kalkulacja.Model);
                koszt0 = sumaZKosztowPrzebiegu + (wynajemWLatach - iloscPrzegladowZPrzebiegu) * KOSZT_PRZEGLADU_PODSTAWOWEGO;
            }
            else
                koszt0 = sumaZKosztowPrzebiegu;

            decimal result = koszt0 + inneKosztySerwisowaniaNetto;

            return result;
        }

        decimal GetKosztyLacznie(int iloscPrzegladowZPrzebiegu, int wynajemWLatach, decimal pakietSerwisowyNetto, decimal pakietSerwisowyKorektaKosztow, decimal sumaZKosztowPrzebiegu, decimal inneKosztySerwisowaniaNetto, decimal korektaAdminProcent)
        {
            if (!(_kalkulacja.CzyUwzgledniaSerwisowanie ?? false))
                return 0.0M;

            decimal kosztyLacznie = 0.0M;

            if (pakietSerwisowyNetto > 0)
            {
                kosztyLacznie = pakietSerwisowyKorektaKosztow;
            }
            else
            {
                decimal k0 = 0;
                if (iloscPrzegladowZPrzebiegu < wynajemWLatach)
                {
                    decimal KOSZT_PRZEGLADU_PODSTAWOWEGO = GetKosztPrzegladuPodstawowego(_kalkulacja.Model);
                    k0 = sumaZKosztowPrzebiegu + (wynajemWLatach - iloscPrzegladowZPrzebiegu) * KOSZT_PRZEGLADU_PODSTAWOWEGO;
                }
                else
                {
                    k0 = sumaZKosztowPrzebiegu;
                }

                kosztyLacznie = k0 + inneKosztySerwisowaniaNetto - korektaAdminProcent;
            }
            return kosztyLacznie;
        }

        #endregion
    }

}
