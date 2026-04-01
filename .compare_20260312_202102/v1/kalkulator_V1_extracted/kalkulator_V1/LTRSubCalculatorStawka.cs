using Express.Logic.Kalkulatory.LTR.CalculatorReport;
using Express.WD;
using System.Collections.Generic;
using System.Linq;

namespace Express.Logic.Kalkulatory.LTR
{
    public class LTRSubCalculatorStawka : LTRSubCalculator
    {
        public class Data : LTRSubCalculatorData
        {
            public decimal KosztMC { get; set; }
            public decimal KosztMcBEZcz { get; set; }

            public decimal UtrataWartosciNetto { get; set; }
            public decimal KosztyFinansoweNetto { get; set; }
            public decimal UbezpieczenieNetto { get; set; }
            public decimal SamochodZastepczyNetto { get; set; }
            public decimal KosztyDodatkoweNetto { get; set; }
            public decimal OponyNetto { get; set; }
            public decimal SerwisNetto { get; set; }
        }


        public class Result : LTRSubCalculatorResult
        {
            public decimal CzynszFinansowy { get; set; }
            public decimal CzynszTechniczny { get; set; }
            public decimal OferowanaStawka { get; set; }
            public decimal PodstawaMarzy { get; set; }
            public decimal Przychod { get; set; }
            public decimal Ubezpieczenie { get; set; }
            public decimal Serwis { get; set; }
            public decimal Admin { get; set; }
            public decimal Opony { get; set; }
            public decimal SamochodZastepczy { get; set; }
            public decimal MarzaNaKontrakcie { get; set; }
            public decimal MarzaNaKontrakcieProcent { get; set; }
            public decimal MarzaMC { get; set; }
            public decimal KosztyLaczneMC { get; set; }
            public decimal KosztFinansowyLacznie { get; set; }
            public decimal KosztFinansowyMiesiecznie { get; set; }
            public decimal UbezpieczenieKorekta { get; set; }
            public decimal SerwisKorekta { get; set; }
            public decimal AdminKorekta { get; set; }
            public decimal OponyKorekta { get; set; }
            public decimal SamochodZastepczyKorekta { get; set; }

            public Koszt KosztFinansowy { get; set; }
            public Koszt KosztUbezpieczenie { get; set; }
            public Koszt KosztSamochodZastepczy { get; set; }
            public Koszt KosztSerwis { get; set; }
            public Koszt KosztOpony { get; set; }
            public Koszt KosztAdmin { get; set; }
        }


        public LTRSubCalculatorStawka(Kalkulacja kalkulacja, LTRTabele tabele) : base(kalkulacja, tabele)
        {
        }

        public class Koszt
        {

            public Koszt(string name)
            {
                Name = name;
            }

            public string Name { get; private set; }

            public decimal RozkladMarzy { get; set; }
            public decimal KosztyLaczne { get; set; }
            public decimal KosztMC { get; set; }
            public decimal MarzaDlaKalk { get; set; }
            public decimal KwotaMarzy { get; set; }
            public decimal KosztPlusMarza { get; set; }
            public decimal RozkladMarzyKorekta { get; set; }
            public decimal KwotaMarzyKorekta { get; set; }
            public decimal KosztPlusMarzaKorekta { get; set; }
        }

        public Result Policz(Data data)
        {
           

            decimal marza = Kwota.GetAsDecimal(_kalkulacja.Marza);
            if ((1 - marza) == 0.0m)
                throw new TF.BusinessFramework.Exceptions.TFBusinessFrameworkInvalidOperationException("Nie można przeliczyć ponieważ 1 - marza = 0");

            decimal kosztyLaczneMC = GetKosztyLaczneMC(data);            
            decimal podstawaMarzy = 0;
            
            if (Kwota.GetAsDecimal(_kalkulacja.CzynszInicjalny) == 0)
            {
                podstawaMarzy = data.KosztMC;                
            }
            else
            {
                podstawaMarzy = data.KosztMcBEZcz;                
            }

            decimal marzaMC = podstawaMarzy * (1 / (1 - marza)) - podstawaMarzy;

            decimal marzaNaKontrakcie = marzaMC * _tabele.Okres;

            Koszt kosztFinansowy = new Koszt("Koszt finansowy");
            kosztFinansowy.KosztyLaczne = data.KosztyFinansoweNetto + data.UtrataWartosciNetto;
            if ((_tabele.Okres) == 0.0m)
                throw new TF.BusinessFramework.Exceptions.TFBusinessFrameworkInvalidOperationException("Nie można przeliczyć ponieważ _tabele.Okres = 0");
            kosztFinansowy.KosztMC = kosztFinansowy.KosztyLaczne / _tabele.Okres;
            if ((kosztyLaczneMC) == 0.0m)
                throw new TF.BusinessFramework.Exceptions.TFBusinessFrameworkInvalidOperationException("Nie można przeliczyć ponieważ kosztyLaczneMC = 0");
            kosztFinansowy.RozkladMarzy = kosztFinansowy.KosztMC / kosztyLaczneMC;
            kosztFinansowy.RozkladMarzyKorekta = _kalkulacja.MarzaKosztFinansowyProcent.HasValue ? _kalkulacja.MarzaKosztFinansowyProcent.Value : kosztFinansowy.KosztMC / kosztyLaczneMC;
            kosztFinansowy.MarzaDlaKalk = kosztFinansowy.RozkladMarzyKorekta;
            kosztFinansowy.KwotaMarzy = marzaMC * kosztFinansowy.RozkladMarzy;
            kosztFinansowy.KwotaMarzyKorekta = marzaMC * kosztFinansowy.RozkladMarzyKorekta;
            kosztFinansowy.KosztPlusMarza = kosztFinansowy.KosztMC + kosztFinansowy.KwotaMarzy;
            kosztFinansowy.KosztPlusMarzaKorekta = kosztFinansowy.KosztMC + kosztFinansowy.KwotaMarzyKorekta;

            Koszt kosztUbezpieczenie = new Koszt("Ubezpieczenie");
            kosztUbezpieczenie.KosztyLaczne = data.UbezpieczenieNetto;
            kosztUbezpieczenie.KosztMC = kosztUbezpieczenie.KosztyLaczne / _tabele.Okres;
            kosztUbezpieczenie.RozkladMarzy = kosztUbezpieczenie.KosztMC / kosztyLaczneMC;
            kosztUbezpieczenie.RozkladMarzyKorekta = _kalkulacja.MarzaUbezpieczenieProcent.HasValue ? _kalkulacja.MarzaUbezpieczenieProcent.Value : kosztUbezpieczenie.KosztMC / kosztyLaczneMC;
            kosztUbezpieczenie.MarzaDlaKalk = (kosztUbezpieczenie.KosztMC == 0) ? 0 : kosztUbezpieczenie.RozkladMarzyKorekta;
            kosztUbezpieczenie.KwotaMarzy = marzaMC * kosztUbezpieczenie.RozkladMarzy;
            kosztUbezpieczenie.KwotaMarzyKorekta = marzaMC * kosztUbezpieczenie.RozkladMarzyKorekta;
            kosztUbezpieczenie.KosztPlusMarza = kosztUbezpieczenie.KosztMC + kosztUbezpieczenie.KwotaMarzy;
            kosztUbezpieczenie.KosztPlusMarzaKorekta = kosztUbezpieczenie.KosztMC + kosztUbezpieczenie.KwotaMarzyKorekta;

            Koszt kosztSamochodZastepczy = new Koszt("Samochód zastępczy");
            kosztSamochodZastepczy.KosztyLaczne = data.SamochodZastepczyNetto;
            kosztSamochodZastepczy.KosztMC = kosztSamochodZastepczy.KosztyLaczne / _tabele.Okres;
            kosztSamochodZastepczy.RozkladMarzy = kosztSamochodZastepczy.KosztMC / kosztyLaczneMC;
            kosztSamochodZastepczy.RozkladMarzyKorekta = _kalkulacja.MarzaSamochodZastepczyProcent.HasValue ? _kalkulacja.MarzaSamochodZastepczyProcent.Value : kosztSamochodZastepczy.KosztMC / kosztyLaczneMC;
            kosztSamochodZastepczy.MarzaDlaKalk = kosztSamochodZastepczy.RozkladMarzyKorekta;
            kosztSamochodZastepczy.KwotaMarzy = marzaMC * kosztSamochodZastepczy.RozkladMarzy;
            kosztSamochodZastepczy.KwotaMarzyKorekta = marzaMC * kosztSamochodZastepczy.RozkladMarzyKorekta;
            kosztSamochodZastepczy.KosztPlusMarza = kosztSamochodZastepczy.KosztMC + kosztSamochodZastepczy.KwotaMarzy;
            kosztSamochodZastepczy.KosztPlusMarzaKorekta = kosztSamochodZastepczy.KosztMC + kosztSamochodZastepczy.KwotaMarzyKorekta;

            Koszt kosztSerwis = new Koszt("Serwis");
            kosztSerwis.KosztyLaczne = data.SerwisNetto;
            kosztSerwis.KosztMC = kosztSerwis.KosztyLaczne / _tabele.Okres;
            kosztSerwis.RozkladMarzy = kosztSerwis.KosztMC / kosztyLaczneMC;
            kosztSerwis.RozkladMarzyKorekta = _kalkulacja.MarzaSerwisProcent.HasValue ? _kalkulacja.MarzaSerwisProcent.Value : kosztSerwis.KosztMC / kosztyLaczneMC;
            kosztSerwis.MarzaDlaKalk = kosztSerwis.RozkladMarzyKorekta;
            kosztSerwis.KwotaMarzy = marzaMC * kosztSerwis.RozkladMarzy;
            kosztSerwis.KwotaMarzyKorekta = marzaMC * kosztSerwis.RozkladMarzyKorekta;
            kosztSerwis.KosztPlusMarza = kosztSerwis.KosztMC + kosztSerwis.KwotaMarzy;
            kosztSerwis.KosztPlusMarzaKorekta = kosztSerwis.KosztMC + kosztSerwis.KwotaMarzyKorekta;

            Koszt kosztOpony = new Koszt("Opony");
            kosztOpony.KosztyLaczne = data.OponyNetto;
            kosztOpony.KosztMC = kosztOpony.KosztyLaczne / _tabele.Okres;
            kosztOpony.RozkladMarzy = kosztOpony.KosztMC / kosztyLaczneMC;
            kosztOpony.RozkladMarzyKorekta = _kalkulacja.MarzaOponyProcent.HasValue ? _kalkulacja.MarzaOponyProcent.Value : kosztOpony.KosztMC / kosztyLaczneMC;
            kosztOpony.MarzaDlaKalk = kosztOpony.RozkladMarzyKorekta;
            kosztOpony.KwotaMarzy = marzaMC * kosztOpony.RozkladMarzy;
            kosztOpony.KwotaMarzyKorekta = marzaMC * kosztOpony.RozkladMarzyKorekta;
            kosztOpony.KosztPlusMarza = kosztOpony.KosztMC + kosztOpony.KwotaMarzy;
            kosztOpony.KosztPlusMarzaKorekta = kosztOpony.KosztMC + kosztOpony.KwotaMarzyKorekta;

            Koszt kosztAdmin = new Koszt("Serwis admin rej");
            kosztAdmin.KosztyLaczne = data.KosztyDodatkoweNetto;
            kosztAdmin.KosztMC = kosztAdmin.KosztyLaczne / _tabele.Okres;
            kosztAdmin.RozkladMarzy = kosztAdmin.KosztMC / kosztyLaczneMC;
            kosztAdmin.RozkladMarzyKorekta = _kalkulacja.MarzaKosztyDodatkoweProcent.HasValue ? _kalkulacja.MarzaKosztyDodatkoweProcent.Value : kosztAdmin.KosztMC / kosztyLaczneMC;
            kosztAdmin.MarzaDlaKalk = kosztAdmin.RozkladMarzyKorekta;
            kosztAdmin.KwotaMarzy = marzaMC * kosztAdmin.RozkladMarzy;
            kosztAdmin.KwotaMarzyKorekta = marzaMC * kosztAdmin.RozkladMarzyKorekta;
            kosztAdmin.KosztPlusMarza = kosztAdmin.KosztMC + kosztAdmin.KwotaMarzy;
            kosztAdmin.KosztPlusMarzaKorekta = kosztAdmin.KosztMC + kosztAdmin.KwotaMarzyKorekta;

            decimal czynszFinansowy = kosztFinansowy.KosztPlusMarzaKorekta;
            decimal czynszTechniczny = kosztUbezpieczenie.KosztPlusMarzaKorekta +
                                       kosztSamochodZastepczy.KosztPlusMarzaKorekta +
                                       kosztSerwis.KosztPlusMarzaKorekta +
                                       kosztOpony.KosztPlusMarzaKorekta +
                                       kosztAdmin.KosztPlusMarzaKorekta;

            decimal oferowanaStawka = czynszFinansowy + czynszTechniczny;

            decimal przychod = oferowanaStawka * _tabele.Okres;
            if ((przychod) == 0.0m)
                throw new TF.BusinessFramework.Exceptions.TFBusinessFrameworkInvalidOperationException("Nie można przeliczyć ponieważ przychod = 0");

            decimal marzaNaKontrakcieProcent = marzaNaKontrakcie / przychod;


            decimal SYM_marzaMC = data.KosztMC * (1 / (1 - marza)) - data.KosztMC;
            decimal SYM_marzaNaKontrakcie = _kalkulacja.OkresUzytkowania * SYM_marzaMC;
            decimal SYM_przychod = (SYM_marzaMC + data.KosztMC) * _kalkulacja.OkresUzytkowania;
            if ((SYM_przychod) == 0.0m)
                throw new TF.BusinessFramework.Exceptions.TFBusinessFrameworkInvalidOperationException("Nie można przeliczyć ponieważ SYM_przychod = 0");
            decimal SYM_marzaNaKontrakcieProcent = SYM_marzaNaKontrakcie / SYM_przychod;


            if (_tabele.IsReport)
            {

                List<string> headers = new List<string>() { "NETTO", "Marza symulowana (bez uwzglednienia CZ)" };
                CalcReportTable marzaNewTable = new CalcReportTable("MARZA NA KONTRAKCIE", headers);
                _tabele.Report.AddItem(marzaNewTable);
                marzaNewTable.AddRow("marzaMC", new List<decimal>() { marzaMC, SYM_marzaMC });
                marzaNewTable.AddRow("marzaNaKontrakcie", new List<decimal>() { marzaNaKontrakcie, SYM_marzaNaKontrakcie });
                marzaNewTable.AddRow("marzaNaKontrakcieProcent", new List<decimal>() { marzaNaKontrakcieProcent, SYM_marzaNaKontrakcieProcent });
                marzaNewTable.AddRow("przychod", new List<decimal>() { przychod, SYM_przychod });

                List<Koszt> koszty = new List<Koszt>() { kosztFinansowy, kosztUbezpieczenie, kosztSamochodZastepczy, kosztSerwis, kosztOpony, kosztAdmin };
                CalcReportTable table = getReportTable(koszty);
                _tabele.Report.AddItem(table);

                CalcReportNameValueTable stawkaTable = new CalcReportNameValueTable("OFEROWANA STAWKA");
                _tabele.Report.AddItem(stawkaTable);

                stawkaTable.AddValue("czynsz finansowy", czynszFinansowy);
                stawkaTable.AddValue("czynszTechniczny", czynszTechniczny);
                stawkaTable.AddValue("ubezpieczenie", kosztUbezpieczenie.KosztPlusMarzaKorekta);
                stawkaTable.AddValue("samochód zastępczy", kosztSamochodZastepczy.KosztPlusMarzaKorekta);
                stawkaTable.AddValue("serwis", kosztSerwis.KosztPlusMarzaKorekta);
                stawkaTable.AddValue("opony (łączny koszt opon na kontrakt)", kosztOpony.KosztPlusMarzaKorekta);
                stawkaTable.AddValue("koszty dodatkowe", kosztAdmin.KosztPlusMarzaKorekta);
            }

            return new Result()
            {
                CzynszFinansowy = czynszFinansowy,
                CzynszTechniczny = czynszTechniczny,
                Przychod = przychod,
                PodstawaMarzy = podstawaMarzy,
                OferowanaStawka = oferowanaStawka,
                Ubezpieczenie = kosztUbezpieczenie.KosztPlusMarza,
                Serwis = kosztSerwis.KosztPlusMarza,
                Admin = kosztAdmin.KosztPlusMarza,
                Opony = kosztOpony.KosztPlusMarza,
                SamochodZastepczy = kosztSamochodZastepczy.KosztPlusMarza,
                UbezpieczenieKorekta = kosztUbezpieczenie.KosztPlusMarzaKorekta,
                SerwisKorekta = kosztSerwis.KosztPlusMarzaKorekta,
                AdminKorekta = kosztAdmin.KosztPlusMarzaKorekta,
                OponyKorekta = kosztOpony.KosztPlusMarzaKorekta,
                SamochodZastepczyKorekta = kosztSamochodZastepczy.KosztPlusMarzaKorekta,
                MarzaNaKontrakcie = marzaNaKontrakcie,
                MarzaNaKontrakcieProcent = marzaNaKontrakcieProcent,
                MarzaMC = marzaMC,
                KosztyLaczneMC = kosztyLaczneMC,
                KosztFinansowyLacznie = kosztFinansowy.KosztyLaczne,
                KosztFinansowyMiesiecznie = kosztFinansowy.KosztMC,

                KosztFinansowy = kosztFinansowy,
                KosztUbezpieczenie = kosztUbezpieczenie,
                KosztOpony = kosztOpony,
                KosztSamochodZastepczy = kosztSamochodZastepczy,
                KosztSerwis = kosztSerwis,
                KosztAdmin = kosztAdmin
            };

        }

        CalcReportTable getReportTable(List<Koszt> koszty)
        {

            try
            {

                List<string> tableHeaders = koszty.Select(k => k.Name).ToList();
                CalcReportTable table = new CalcReportTable(string.Empty, tableHeaders);

                table.AddRow("Rozkład marży", koszty.Select(k => k.RozkladMarzy).ToList());
                table.AddRow("Koszty łącznie", koszty.Select(k => k.KosztyLaczne).ToList());
                table.AddRow("Koszt MC", koszty.Select(k => k.KosztMC).ToList());
                table.AddRow("Podział marży", koszty.Select(k => k.RozkladMarzy).ToList());
                table.AddRow("Podział marży ręczny", koszty.Select(k => k.RozkladMarzyKorekta).ToList());
                table.AddRow("Kwota marży", koszty.Select(k => k.KwotaMarzy).ToList());
                table.AddRow("Kwota marży podział ręczny", koszty.Select(k => k.KwotaMarzyKorekta).ToList());
                table.AddRow("Koszt+marża", koszty.Select(k => k.KosztPlusMarza).ToList());
                table.AddRow("Koszt+marża podział ręczny", koszty.Select(k => k.KosztPlusMarzaKorekta).ToList());

                table.IsToReverse = true;

                return table;

            }
            catch
            {
                return null;
            }


        }

        private decimal GetKosztyLaczneMC(Data data)
        {
            decimal result = 0.0M;

            result += data.KosztyFinansoweNetto + data.UtrataWartosciNetto;
            result += data.UbezpieczenieNetto;
            result += data.SamochodZastepczyNetto;
            result += data.SerwisNetto;
            result += data.OponyNetto;
            result += data.KosztyDodatkoweNetto;

            result /= _tabele.Okres;

            return result;
        }
    }
}
