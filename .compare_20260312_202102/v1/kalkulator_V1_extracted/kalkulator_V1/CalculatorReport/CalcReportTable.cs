using System.Collections.Generic;

namespace Express.Logic.Kalkulatory.LTR.CalculatorReport
{
    public class CalcReportTable : CalcReportItem
    {

        #region <const>
      //  const string FORMAT_DECIMAL = "0.0000";
        #endregion

        #region <members>
        int _numberOfColumns;
        #endregion

        #region <props>
        public List<List<string>> ListOfTableItems { get; set; }
        public bool IsToReverse { get; set; }
        public string Title { get; set; }
        #endregion

        #region <ctor>
        public CalcReportTable(string title, List<string> rowHeaders)
        {
            Title = title;
            TypeOfItem = CalcReportItemType.Table;
            _numberOfColumns = rowHeaders.Count;
            ListOfTableItems = new List<List<string>>();
            AddRow(string.Empty, rowHeaders);
        }
        #endregion

        #region <public methods>

        public void AddRow(string rowName, List<string> values)
        {
            if (values.Count != _numberOfColumns) return;

            List<string> listItem = new List<string>() { rowName };
            values.ForEach(v => listItem.Add(v));

            ListOfTableItems.Add(listItem);

        }

        public void AddRow(string rowName, List<decimal> values)
        {
            if (values.Count != _numberOfColumns) return;

            List<string> listItem = new List<string>() { rowName };
            values.ForEach(v => listItem.Add(v.ToString(FORMAT_DECIMAL)));

            ListOfTableItems.Add(listItem);
        }

        public void AddRow(string rowName, List<int> values)
        {
            if (values.Count != _numberOfColumns) return;

            List<string> listItem = new List<string>() { rowName };
            values.ForEach(v => listItem.Add(v.ToString()));

            ListOfTableItems.Add(listItem);
        }

        #endregion

    }

}
