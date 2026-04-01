using Express.BaseTypes;
using Express.WD;
using System;
using System.Linq;
using TF.BusinessFramework.Patterns.UnitOfWork;
using TF.BusinessFramework.Utilities;

namespace Express.Logic.Kalkulatory.LTR.Cache
{
    internal static class LTRAdminParamsSerwisCache
    {
        internal static readonly ExpiringDictionary<string, decimal> LTRAdminKosztySerwisoweDictionary = new ExpiringDictionary<string, decimal>(TimeSpan.FromHours(1));

        internal static decimal GetLTRAdminKosztySerwisowe(int klasaId, int markaId, Fuel rodzajPaliwa, string rodzajKosztow, IUnitOfWork unitOfWork)
        {
            return unitOfWork.GetRepository<LTRAdminKosztySerwisowe>().AsQueryable().Where(e => e.KlasaId == klasaId && e.MarkaId == markaId && e.RodzajPaliwa == rodzajPaliwa && e.RodzajKosztow == rodzajKosztow).OrderByDescending(e => e.Id).Select(e => e.StawkaZaKm).FirstOrDefault();
        }

        internal static string LTRAdminKosztySerwisoweDescriptor(int klasaId, int markaId, Fuel rodzajPaliwa, string rodzajKosztow)
        {
            return $"{klasaId};{markaId};{rodzajPaliwa};{rodzajKosztow}";
        }
    }
}
