using Express.WD;
using System;
using System.Collections.Generic;
using System.Linq;
using TF.BusinessFramework.Patterns.UnitOfWork;
using TF.BusinessFramework.Utilities;

namespace Express.Logic.Kalkulatory.LTR.Cache
{
    public static class LTRAdminParametryCache
    {
        internal static readonly ExpiringCache<LTRParametry> LTRParametryCache = new ExpiringCache<LTRParametry>(TimeSpan.FromHours(1));

        public static LTRParametry GetLTRParametry(IUnitOfWork unitOfWork)
        {
            return LTRParametryCache.Get(() =>
            {
                Dictionary<string, string> paramDict = unitOfWork.GetRepository<LTRAdminParametry>().AsQueryable().ToDictionary(p => p.Nazwa, p => p.Wartosc);
                return new LTRParametry(paramDict);
            });
        }
    }
}
