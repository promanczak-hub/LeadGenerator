using Express.Logic.Kalkulatory.LTR.CalculatorReport;
using Express.WD;
using System.Collections.Generic;
using TF.BusinessFramework.Patterns.UnitOfWork;

namespace Express.Logic.Kalkulatory.LTR
{
    public class LTRTabele
    {
        public LTRTabele(IUnitOfWork iUnitOfWork)
        {
            this.iUnitOfWork = iUnitOfWork;
        }

        public int CennikOponId { get; set; }

        public IUnitOfWork iUnitOfWork { get; set; }

        public IEnumerable<LTRKorektaEtax> KorektyEtax { get; set; }
        public IEnumerable<LTRAdminKorektyRvDsu> KorektyRvDsu { get; set; }
        public IEnumerable<LTRAdminOplatyTransportowe> OplatyTransportowe { get; set; }

        public IEnumerable<LTRPrzebiegNormatywny> PrzebiegNormatywny { get; set; }
        public IEnumerable<LTRAdminUbezpieczenie> Ubezpieczenie { get; set; }
        public IEnumerable<LTRAdminWspolczynnikiSzkodowe> WspolczynnikiSzkodowe { get; set; }
        public IEnumerable<LTRAdminStawkaZastepczy> StawkaZastepczy { get; set; }
        public IEnumerable<LTRAdminGSM> AbonamentGSM { get; set; }
        public IEnumerable<LTRAdminKategoriaKorekta> KategoriaKorekta { get; set; }
        public IEnumerable<LTRAdminPrzebiegKorekta> PrzebiegKorekta { get; set; }
        public IEnumerable<LTRAdminKosztZabudowy> KosztZabudowy { get; set; }

      

        public IEnumerable<TabelaEuroTax> TabeleEurotax { get; set; }
        public IEnumerable<WRLTR> WRLTRTable { get; set; }

        public LTRParametry Parametry { get; set; }

        public CalcReport Report { get; set; }

        public bool IsReport { get; set; }

        public int Przebieg { get; set; }
        public int PrzebiegSkorygowany { get; set; }
        public int Okres { get; set; }
    }
}
