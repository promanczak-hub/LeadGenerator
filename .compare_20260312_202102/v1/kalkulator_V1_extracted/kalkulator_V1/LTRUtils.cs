using Express.WD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Express.Logic.Kalkulatory.LTR
{
    public class LTRUtils
    {

        #region <singleton>
        LTRUtils() { }

        static LTRUtils _instance;

        public static LTRUtils Instance
        {
            get
            {
                if (null == _instance) _instance = new LTRUtils();
                return _instance;
            }
        }
        #endregion


        #region <pub>

        public T GetBySymbolEurotax<T>(IEnumerable<T> items, Kalkulacja kalkulacja)
        {


            try
            {

                if (null == kalkulacja || null == kalkulacja.Model || null == kalkulacja.Model.SymbolEuroTax) return default(T);

                PropertyInfo[] props = typeof(T).GetProperties();
                bool isSymbolEuroTaxId = props.Any(p => p.Name == "SymbolEuroTaxId");
                bool isSymbolEuroTax = props.Any(p => p.Name == "SymbolEuroTax");


                if (isSymbolEuroTaxId)
                {
                    int id = kalkulacja.Model.SymbolEuroTax.Id;
                    foreach (T item in items)
                    {

                        PropertyInfo idInfo = typeof(T).GetProperty("SymbolEuroTaxId");
                        if (null != idInfo)
                        {
                            int itemEurotaxId = (int)idInfo.GetValue(item);
                            if (itemEurotaxId == id)
                                return item;
                        }

                    }
                }


                if (isSymbolEuroTax)
                {

                    string baseSymbol = null;
                    string symbol = kalkulacja.Model.SymbolEuroTax.Symbol;
                    if (symbol != null && symbol.Length > 2)
                    {
                        baseSymbol = kalkulacja.Model.SymbolEuroTax.Symbol.Substring(0, 2);
                    }

                    if (null != baseSymbol)
                    {
                        foreach (T item in items)
                        {

                            PropertyInfo idInfo = typeof(T).GetProperty("SymbolEuroTax");
                            if (null != idInfo)
                            {
                                SymbolEuroTax symbolET = (SymbolEuroTax)idInfo.GetValue(item);
                                if (null != symbolET)
                                {

                                    if (symbolET.Symbol == baseSymbol)
                                        return item;
                                }
                            }
                        }
                    }
                }



                

            }
            catch(Exception )
            {


            }


            return default(T);

        }

        #endregion




    }
}
