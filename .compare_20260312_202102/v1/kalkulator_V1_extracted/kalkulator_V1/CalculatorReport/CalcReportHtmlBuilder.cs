using Express.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Express.Logic.Kalkulatory.LTR.CalculatorReport
{

    public class CalcReportHtmlBuilder
    {


        #region <const>

        const string TAG_TABLE_START = "<TABLE border=\"1\" cellpadding=\"0\" cellspacing=\"0\">";
        const string TAG_TABLE_END = "</TABLE><br>";

        #endregion

        #region <singleton>
        CalcReportHtmlBuilder() { }

        static CalcReportHtmlBuilder _instance;

        public static CalcReportHtmlBuilder Instance
        {
            get
            {
                if (null == _instance) _instance = new CalcReportHtmlBuilder();
                return _instance;
            }
        }
        #endregion			

        #region <public methods>
        public string BuildHTML(CalcReport report)
        {


            try
            {


                StringBuilder htmlBuilder = new StringBuilder();


                foreach (CalcReportItem item in report.Items)
                {

                    if (null == item) continue;
                    
                    if (item is CalcReportTable)
                    {
                        string tableAsHtml = getTableAsHtml((CalcReportTable)item);
                        htmlBuilder.AppendLine(tableAsHtml);
                    }
                    else if (item is CalcReportNameValueTable)
                    {
                        string tableAsHtml = getNameValueTableAsHtml((CalcReportNameValueTable)item);
                        htmlBuilder.AppendLine(tableAsHtml);
                    }
                    else
                    {
                        string itemAsHtml = getItemAsHtml(item);
                        htmlBuilder.AppendLine(itemAsHtml);
                    }

                }

                return htmlBuilder.ToString();
            }
            catch
            {
                return string.Empty;
            }

        }

        private string getItemAsHtml(CalcReportItem item)
        {

            string atts = string.Empty;
            if (item.TypeOfItem == CalcReportItemType.Error)
                atts = "style =\"color: red\"";


            StringBuilder builder = new StringBuilder();
            builder.Append("<p %atts>".Replace("%atts", atts));
            builder.Append(item.Value);
            builder.Append("</p>");

            string html = builder.ToString();
            return html;


        }
        #endregion

        #region <private method>
        string getTableAsHtml(CalcReportTable table)
        {

            try
            {

                StringBuilder tableBuilder = new StringBuilder();

                tableBuilder.AppendLine(TAG_TABLE_START);


                List<List<string>> items = (table.IsToReverse) ? 
                    getTableReversed(table.ListOfTableItems) : 
                    table.ListOfTableItems;

                if (!string.IsNullOrEmpty(table.Title))
                {
                    int numberOfColumns = items.First().Count;
                    
                    tableBuilder.AppendLine("<tr>");

                    string cellTemplate = "<td colspan=\"{0}\">{1}</td>";
                    string cell = string.Format(cellTemplate, numberOfColumns, table.Title);
                    tableBuilder.AppendLine(cell);
                    
                    tableBuilder.AppendLine("</tr>");


                }


                foreach (List<string> item in items)
                {
                    tableBuilder.AppendLine("<tr>");

                    foreach (string cell in item)
                    {
                        tableBuilder.Append("<td>");
                        tableBuilder.Append(cell);
                        tableBuilder.Append("</td>");
                    }

                    tableBuilder.AppendLine("</tr>");
                }


                tableBuilder.AppendLine(TAG_TABLE_END);

                string tableAsHtml = tableBuilder.ToString();
                return tableAsHtml;
            }
            catch
            {
                return string.Empty;
            }


        }

        List<List<string>> getTableReversed(List<List<string>> tableToReverse)
        {

            int numberOfRows = tableToReverse.Count;
            if (numberOfRows == 0) return tableToReverse;

            int numberOfColumns = tableToReverse.First().Count;

            List<List<string>> reversed = new List<List<string>>();
            for (int c = 0; c < numberOfColumns; c++)
            {
                List<string> newRow = new List<string>();
                for (int r = 0; r < numberOfRows; r++)
                {
                    string item = tableToReverse[r][c];
                    newRow.Add(item);
                }

                reversed.Add(newRow);
            }

            return reversed;

        }


        string getNameValueTableAsHtml(CalcReportNameValueTable table)
        {
            try
            {

                StringBuilder tableBuilder = new StringBuilder();

                tableBuilder.AppendLine(TAG_TABLE_START);

                if (!string.IsNullOrEmpty(table.Title))
                {
                    tableBuilder.AppendLine("<tr>");
                    tableBuilder.Append("<td colspan = \"2\">");
                    tableBuilder.Append(getBoldIfNecessary(table.Title, true));
                    tableBuilder.Append("</td>");
                    tableBuilder.AppendLine("</tr>");
                }


                foreach (CalcReportNameValueTable.Item item in table.Items)
                {
                    if (null == item) continue;

                    tableBuilder.AppendLine("<tr>");
                    tableBuilder.Append("<td>");
                    tableBuilder.Append(getBoldIfNecessary(item.Name, item.IsBold));
                    tableBuilder.Append("</td>");
                    tableBuilder.Append("<td>");
                    tableBuilder.Append(getBoldIfNecessary(item.Value, item.IsBold));
                    tableBuilder.Append("</td>");
                    tableBuilder.AppendLine("</tr>");
                }

                tableBuilder.AppendLine(TAG_TABLE_END);

                string tableAsHtml = tableBuilder.ToString();
                return tableAsHtml;
            }
            catch
            {
                return string.Empty;
            }
         
        }

        string getBoldIfNecessary(string value, bool isBold)
        {
            if (!isBold)
                return value;
            else
                return "<b>" + value + "</b>";

        }

        
        
        
        #endregion


    }

}
