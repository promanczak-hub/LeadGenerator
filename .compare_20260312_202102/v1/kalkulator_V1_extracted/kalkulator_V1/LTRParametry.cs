using Express.WD;
using System.Collections.Generic;
using System.Reflection;

namespace Express.Logic.Kalkulatory.LTR
{
    public class LTRParametry
    {
        #region <constructor>

        public LTRParametry()
        {

        }

        public LTRParametry(Dictionary<string, string> paramsDictionary)
        {
            foreach (string propertyName in paramsDictionary.Keys)
            {
                PropertyInfo pi = GetType().GetProperty(propertyName);
                if (pi == null) continue;
                string value = paramsDictionary[propertyName];
                object valueToSet = null;
                if (pi.PropertyType.Equals(typeof(Kwota)))
                {
                    decimal decValue = decimal.Parse(value);
                    valueToSet = new Kwota(decValue);
                }
                else if (pi.PropertyType.Equals(typeof(int)))
                    valueToSet = int.Parse(value);
                else if (pi.PropertyType.Equals(typeof(decimal)))
                {
                    /*if (value.StartsWith("-"))
                    {
                        value = value.Substring(1);
                        valueToSet = -1 * decimal.Parse(value);
                    }
                    else*/
                    valueToSet = decimal.Parse(value);
                }
                else if (pi.PropertyType.Equals(typeof(bool)))
                    valueToSet = bool.Parse(value);
                else if (pi.PropertyType.Equals(typeof(string)))
                    valueToSet = value;

                pi.SetValue(this, valueToSet);
            }
        }
        #endregion

        #region <properties>
        public decimal StawkaVAT { get; set; }
        public bool UzywajNowegoKalkulatoraWR { get; set; }
        public bool UzywajNowegoKalkulatoraSerwis { get; set; }

        // dane techniczne
        public Kwota OponyPrzekladki { get; set; }
        public Kwota OponyPrzechowywane { get; set; }
        public Kwota ZarejestrowanieKartaPojazdu { get; set; }
        public Kwota HakHolowniczy { get; set; }
        public Kwota KosztWymontowaniaKraty { get; set; }
        public Kwota PrzygotowanieDoSprzedazyLtr { get; set; }
        public Kwota PrzygotowanieDoSprzedazyRacMtr { get; set; }
        public Kwota StawkaZaZastepczy { get; set; }
        public Kwota ProgKwotowyDlaDrugiegoZabezpieczenia { get; set; }
        public Kwota ProgKwotowyDlaTrzeciegoZabezpieczenia { get; set; }
        public Kwota AbonamentGPS { get; set; }
        public Kwota CenaUrzadzeniaGSM { get; set; }
        public Kwota MontazUrzadzeniaGSM { get; set; }

        // parametry do ubezpieczenia
        public decimal DoubezpieczenieKradziezy { get; set; }
        public decimal NaukaJazdy { get; set; }

        // korekty szkodowe do ubezpieczenia
        public Kwota SredniaWartoscSzkody { get; set; }
        public int SredniPrzebiegDlaSzkody { get; set; }

        // dane finansowe
        public decimal WIBOR { get; set; }
        public decimal MarzaFinansowa { get; set; }
        public decimal KursEURO { get; set; }
        public Kwota ProgWartosciFISKAL { get; set; }
        public decimal StopaPodatkowa { get; set; }
        public decimal ProwizjaMarketingowa { get; set; }
        public decimal MinimalnyProcentAmortyzacji { get; set; }

        // inne korekty RV
        public decimal KorektaNaukaJazdy { get; set; }
        public decimal PrzewidywanaCenaSprzedazyLO { get; set; }
        public decimal UtrataWartosciPrzyBrakuSerwisowania { get; set; }
        public decimal BudzetMarketingowyLtr { get; set; }
        public decimal MaksKorektaWRBMProcent { get; set; }
        // korekty serwis
        public decimal KorektaSerwisProcent { get; set; }
        public int KorektaPrzebieguDlaLTR { get; set; }
        public int CzasPrzygotowaniaDoSprzedazy { get; set; }
        // AP
        public decimal APWR12Months { get; set; }
        public decimal StawkaPolisaZielonaKarta { get; set; }
        public int MocSilnikaDoAkceptacjiDT { get; set; }
        public int DopuszczalnyWiekSamochoduWMiesiacach { get; set; }

        /*  // rozklad marzy
          public decimal MarzaKosztFinansowyProcent { get; set; }
          public decimal MarzaUbezpieczenieProcent { get; set; }
          public decimal MarzaSamochodZastepczyProcent { get; set; }
          public decimal MarzaSerwisProcent { get; set; }*/

        #endregion

        #region <public methods>
        public Dictionary<string, string> GetAsDictionary()
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            foreach (PropertyInfo pi in this.GetType().GetProperties())
            {
                try
                {
                    string name = pi.Name;
                    if (pi.PropertyType.Equals(typeof(Kwota)))
                    {
                        Kwota kwota = (Kwota)pi.GetValue(this);
                        if (null != kwota)
                        {
                            string wartosc = kwota.Wartosc.ToString();
                            dictionary.Add(name, wartosc);
                        }
                        continue;
                    }

                    string value = pi.GetValue(this).ToString();
                    dictionary.Add(name, value);
                }
                catch
                {
                    continue;
                }
            }
            return dictionary;
        }
        #endregion
    }
}
