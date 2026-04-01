using System;

namespace Express.Logic.Kalkulatory.LTR
{
    public class PMT
    {

        public class Result
        {
            public decimal OplataLeasingowa { get; set; }
            public decimal CzescKapitalowa { get; set; }
            public decimal CzescOdsetkowa { get; set; }
        }

        public static Result GetResult(decimal kapitalDoSplaty, decimal wykup, decimal kf, int nrRaty, int liczbaRat, int m = 12)
        {

            kapitalDoSplaty = kapitalDoSplaty * (-1m);

            int pozostaloRat = liczbaRat + 1 - nrRaty;

            decimal im = (kf / m);
            decimal imN = Pow(1 + im, pozostaloRat);

            if (imN == 1m)
                throw new Exception("Nie można policzyć PMT");

            decimal pmt = ((kapitalDoSplaty * imN + wykup) * im) / (1 - imN);
            decimal cz_odsetkowa = kapitalDoSplaty * im * (-1m);
            decimal cz_kapitalowa = pmt - cz_odsetkowa;

            return new Result()
            {
                OplataLeasingowa = pmt,
                CzescKapitalowa = cz_kapitalowa,
                CzescOdsetkowa = cz_odsetkowa
            };

        }

        static decimal Pow(decimal x, int y)
        {
            decimal result = x;
            for (int i = 1; i < y; i++)
                result *= x;

            return result;
        }
    }
}
