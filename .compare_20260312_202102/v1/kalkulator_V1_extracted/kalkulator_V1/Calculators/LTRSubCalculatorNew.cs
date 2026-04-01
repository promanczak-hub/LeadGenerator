using Express.BaseTypes;
using Express.Logic.Kalkulatory.LTR.CalculatorReport;
using Express.Logic.Kalkulatory.LTR.CalculatorTypes;
using Express.Types.Samochody;
using Express.WD;
using TF.BusinessFramework.Patterns.UnitOfWork;

namespace Express.Logic.Kalkulatory.LTR.Calculators
{
    internal abstract class LTRSubCalculatorNew<I, O>
        where I : LTRSubCalculatorNewInput
        where O : LTRSubCalculatorNewOutput
    {
        public LTRSubCalculatorNew()
        {
        }

        public abstract O Calculate(Kalkulacja kalkulacja, I input, CalcReport report, IUnitOfWork unitOfWork);

        protected Fuel Kalkulacja2Fuel(Kalkulacja kalkulacja)
        {
            switch (kalkulacja.RodzajPaliwa)
            {
                case SamochodRodzajPaliwaConsts.PB:                
                case SamochodRodzajPaliwaConsts.MildHybrid:
                    return Fuel.Gas;
                case SamochodRodzajPaliwaConsts.ON:
                    return Fuel.Diesel;
                case SamochodRodzajPaliwaConsts.Hybrydowy:
                    return Fuel.Hybrid;
                case SamochodRodzajPaliwaConsts.HybrydowyPlugIn:
                    return Fuel.HybridPlugIn;
                case SamochodRodzajPaliwaConsts.Elektryczny:
                    return Fuel.Electric;
                case SamochodRodzajPaliwaConsts.PB_LPG:
                    return Fuel.LPG;
                case SamochodRodzajPaliwaConsts.PB_CNG:
                    return Fuel.CNG;
                default:
                    return Fuel.Unknown;
                
            }
        }

        protected int Kalkulacja2KlasaId(Kalkulacja kalkulacja)
        {
            if (kalkulacja.Model == null)
                throw new TF.BusinessFramework.Exceptions.TFBusinessFrameworkInvalidOperationException("Brak modelu na kalkulacji");
            if (kalkulacja.Model.KlasaId == null)
                throw new TF.BusinessFramework.Exceptions.TFBusinessFrameworkInvalidOperationException("Brak klasy na modelu kalkulacji");
            return kalkulacja.Model.KlasaId.Value;
        }

        protected int Kalkulacja2KlasaWRId(Kalkulacja kalkulacja)
        {
            if (kalkulacja.Model == null)
                throw new TF.BusinessFramework.Exceptions.TFBusinessFrameworkInvalidOperationException("Brak modelu na kalkulacji");

            if (kalkulacja.Model.KlasaSAMAR == null)
                throw new TF.BusinessFramework.Exceptions.TFBusinessFrameworkInvalidOperationException("Brak klasy SAMAR na modelu kalkulacji");

            return kalkulacja.Model.KlasaSAMAR.KlasaWRId;
        }
    }
}
