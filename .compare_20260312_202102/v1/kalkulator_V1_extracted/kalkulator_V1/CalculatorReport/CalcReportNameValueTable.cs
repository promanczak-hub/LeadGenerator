using Express.WD;
using System.Collections.Generic;

namespace Express.Logic.Kalkulatory.LTR.CalculatorReport
{
    public class CalcReportNameValueTable : CalcReportItem
    {


        #region <nested types>
        public class Item
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public bool IsBold { get; set; }

        }
        #endregion

        #region <ctor>
        public CalcReportNameValueTable(string title)
        {
            Title = title;
            TypeOfItem = CalcReportItemType.Table;
        }
        #endregion

        #region <properties>
        public List<Item> Items = new List<Item>();
        public string Title { get; set; }
        #endregion

        #region <public methods>





        public void AddValue(string name, decimal netto, bool isBold = false)
        {
            Items.Add(new Item()
            {
                Name = name,
                Value = netto.ToString(FORMAT_DECIMAL),
                IsBold = isBold
            });
        }

        public void AddValue(string name, decimal? value)
        {
            AddValue(name, value.GetValue());
        }

        public void AddValue(string name, Kwota kwota)
        {
            decimal value = Kwota.GetAsDecimal(kwota);
            AddValue(name, value);
        }

        public void AddValue(string name, bool? value)
        {
            AddValue(name, value.IsTrue());
        }

        public void AddValue(string name, int number)
        {
            Items.Add(new Item()
            {
                Name = name,
                Value = number.ToString(),
            });
        }

        public void AddValue(string name, string value)
        {
            Items.Add(new Item()
            {
                Name = name,
                Value = value
            });

        }

        public void AddValueMissing(string name)
        {
            Items.Add(new Item()
            {
                Name = name,
                Value = "[brak]"
            });
        }


        public void AddValue(string name, bool value)
        {
            Items.Add(new Item()
            {
                Name = name,
                Value = value ? "v" : string.Empty
            });

        }

      

        #endregion

    }

}
