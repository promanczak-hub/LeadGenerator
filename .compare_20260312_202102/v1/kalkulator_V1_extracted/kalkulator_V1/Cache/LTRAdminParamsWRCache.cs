using Express.BaseTypes;
using Express.WD;
using Express.WD.DTO;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TF.BusinessFramework.Patterns.UnitOfWork;
using TF.BusinessFramework.Utilities;

namespace Express.Logic.Kalkulatory.LTR.Cache
{
    internal static class LTRAdminParamsWRCache
    {
        internal static readonly ExpiringDictionary<string, decimal> LTRAdminKorektaWRRocznikDictionary = new ExpiringDictionary<string, decimal>(TimeSpan.FromHours(1));
        internal static readonly ExpiringDictionary<string, decimal> LTRAdminKorektaWRKolorDictionary = new ExpiringDictionary<string, decimal>(TimeSpan.FromHours(1));
        internal static readonly ExpiringDictionary<string, decimal> LTRAdminKorektaWRZabudowaDictionary = new ExpiringDictionary<string, decimal>(TimeSpan.FromHours(1));
        internal static readonly ExpiringDictionary<int, LTRAdminTabelaWRPrzebiegDTO> LTRAdminTabelaWRPrzebiegDictionary = new ExpiringDictionary<int, LTRAdminTabelaWRPrzebiegDTO>(TimeSpan.FromHours(1));
        internal static readonly ExpiringDictionary<string, decimal> LTRAdminTabelaWRDoposazenieDictionary = new ExpiringDictionary<string, decimal>(TimeSpan.FromHours(1));
        internal static readonly ExpiringDictionary<string, decimal> LTRAdminTabelaWRKlasaDictionary = new ExpiringDictionary<string, decimal>(TimeSpan.FromHours(1));
        internal static readonly ExpiringDictionary<string, decimal> LTRAdminKorektaWRMarkaDictionary = new ExpiringDictionary<string, decimal>(TimeSpan.FromHours(1));
        internal static readonly ExpiringDictionary<string, decimal> LTRAdminTabelaDeprecjacjaOkresDictionary = new ExpiringDictionary<string, decimal>(TimeSpan.FromHours(1));        

        internal static decimal Rocznik2LTRAdminKorektaWRRocznik(string rocznik, IUnitOfWork unitOfWork)
        {
            return unitOfWork.GetRepository<LTRAdminKorektaWRRocznik>().AsQueryable().Where(e => e.Rocznik == rocznik).OrderByDescending(e => e.Id).Select(e => e.KorektaProcent).FirstOrDefault();
        }

        internal static decimal Kolor2LTRAdminKorektaWRKolor(string kolor, IUnitOfWork unitOfWork)
        {
            return unitOfWork.GetRepository<LTRAdminKorektaWRKolor>().AsQueryable().Where(e => e.Kolor == kolor).OrderByDescending(e => e.Id).Select(e => e.KorektaProcent).FirstOrDefault();
        }

        internal static decimal RodzajZabudowy2LTRAdminKorektaWRZabudowa(string rodzajZabudowy, IUnitOfWork unitOfWork)
        {
            return unitOfWork.GetRepository<LTRAdminKorektaWRZabudowa>().AsQueryable().Where(e => e.RodzajZabudowy == rodzajZabudowy).OrderByDescending(e => e.Id).Select(e => e.KorektaProcent).FirstOrDefault();
        }

        internal static LTRAdminTabelaWRPrzebiegDTO KlasaWRId2LTRAdminTabelaWRPrzebiegDTO(int klasaWRId, IUnitOfWork unitOfWork)
        {
            return unitOfWork.GetRepository<LTRAdminTabelaWRPrzebieg>().AsQueryable().Where(e => e.KlasaWRId == klasaWRId).Select(e => new LTRAdminTabelaWRPrzebiegDTO
            {
                Id = e.Id,
                KlasaWRId = e.KlasaWRId,
                KorektaProcentPonizej190 = e.KorektaProcentPonizej190,
                KorektaProcentPowyzej190 = e.KorektaProcentPowyzej190
            }).OrderByDescending(r => r.Id).FirstOrDefault();
        }

        internal static string LTRAdminTabelaWRDoposazenieDescriptor(int liczbaLat, Fuel rodzajPaliwa, int klasaWRId)
        {
            return $"{liczbaLat};{rodzajPaliwa};{klasaWRId}";
        }

        internal static decimal GetLTRAdminTabelaWRDoposazenie(int liczbaLat, Fuel rodzajPaliwa, int klasaWRId, IUnitOfWork unitOfWork)
        {
            return unitOfWork.GetRepository<LTRAdminTabelaWRDoposazenie>().AsQueryable().Where(e => e.LiczbaLat == liczbaLat && e.RodzajPaliwa == rodzajPaliwa && e.KlasaWRId == klasaWRId).OrderByDescending(e => e.Id).Select(e => e.KorektaProcent).FirstOrDefault();
        }

        internal static string LTRAdminTabelaWRKlasaDescriptor(Fuel rodzajPaliwa, int klasaWRId)
        {
            return $"{rodzajPaliwa};{klasaWRId}";
        }

        internal static decimal GetLTRAdminTabelaWRKlasa(Fuel rodzajPaliwa, int klasaWRId, IUnitOfWork unitOfWork)
        {
            return unitOfWork.GetRepository<LTRAdminTabelaWRKlasa>().AsQueryable().Where(e => e.RodzajPaliwa == rodzajPaliwa && e.KlasaWRId == klasaWRId).OrderByDescending(e => e.Id).Select(e => e.KorektaProcent).FirstOrDefault();
        }

        internal static string LTRAdminKorektaWRMarkaDescriptor(Fuel rodzajPaliwa, int klasaWRId, int markaId)
        {
            return $"{rodzajPaliwa};{klasaWRId};{markaId}";
        }

        internal static decimal GetLTRAdminKorektaWRMarka(Fuel rodzajPaliwa, int klasaWRId, int markaId, IUnitOfWork unitOfWork)
        {
            return unitOfWork.GetRepository<LTRAdminKorektaWRMarka>().AsQueryable().Where(e => e.RodzajPaliwa == rodzajPaliwa && e.KlasaWRId == klasaWRId && e.MarkaId == markaId).OrderByDescending(e => e.Id).Select(e => e.KorektaProcent).FirstOrDefault();
        }

        internal static string LTRAdminTabelaDeprecjacjaOkresDescriptor(Fuel rodzajPaliwa, int klasaWRId, int rok)
        {
            return $"{rodzajPaliwa};{klasaWRId};{rok}";
        }

        internal static decimal GetLTRAdminTabelaDeprecjacjaOkres(Fuel rodzajPaliwa, int klasaWRId, int rok, IUnitOfWork unitOfWork)
        {
            return unitOfWork.GetRepository<LTRAdminTabelaWRDeprecjacja>().AsQueryable().Where(e => e.RodzajPaliwa == rodzajPaliwa && e.KlasaWRId == klasaWRId && e.Rok == rok).OrderByDescending(e => e.Id).Select(e => e.KorektaProcent).FirstOrDefault();
        }       
    }
}
