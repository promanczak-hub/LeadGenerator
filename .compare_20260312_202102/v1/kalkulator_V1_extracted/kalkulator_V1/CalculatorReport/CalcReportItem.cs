namespace Express.Logic.Kalkulatory.LTR.CalculatorReport
{
    public class CalcReportItem
    {

        public const string UNIQUE_NAME_TABELA_SERWISOWA = "UNIQUE_NAME_TABELA_SERWISOWA";
        public const string UNIQUE_NAME_TABELA_EUROTAX = "UNIQUE_NAME_TABELA_EUROTAX";
        public const string UNIQUE_NAME_DANE = "UNIQUE_NAME_DANE";
        public const string UNIQUE_NAME_MAIN_RESULTS = "UNIQUE_NAME_MAIN_RESULTS";
      


        protected const string FORMAT_DECIMAL = "0.0000";

        public string Value { get; set; }
        
        public CalcReportItemType TypeOfItem {get; set;}

        public string UniqueName { get; set; }

        public override string ToString()
        {
            return Value;
        }

    }

}
