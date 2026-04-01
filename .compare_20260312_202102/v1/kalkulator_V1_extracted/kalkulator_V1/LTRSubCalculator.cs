using Express.WD;

namespace Express.Logic.Kalkulatory.LTR
{
    public abstract class LTRSubCalculator
    {
        protected Kalkulacja _kalkulacja;
        protected LTRTabele _tabele;

        public LTRSubCalculator(Kalkulacja kalkulacja, LTRTabele tabele)
        {
            _kalkulacja = kalkulacja;
            _tabele = tabele;
        }

   //     public abstract LTRSubCalculatorResult Policz(LTRSubCalculatorData calculatorData);
    }
}
