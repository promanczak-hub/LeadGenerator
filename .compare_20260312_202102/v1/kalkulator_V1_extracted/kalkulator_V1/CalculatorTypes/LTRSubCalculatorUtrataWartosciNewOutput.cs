namespace Express.Logic.Kalkulatory.LTR.CalculatorTypes
{
    public class LTRSubCalculatorUtrataWartosciNewOutput : LTRSubCalculatorNewOutput
    {
        public decimal UtrataWartosciBEZczynszu { get; set; }
        public decimal UtrataWartosciZCzynszemInicjalnym { get; set; }
        public decimal KorektaZaPrzebiegKwotowo { get; set; }
        public decimal KorektaAdministracyjnaKwotowo { get; set; }
        public decimal WR { get; set; }
        public decimal WRdlaLO { get; set; }

    }
}
