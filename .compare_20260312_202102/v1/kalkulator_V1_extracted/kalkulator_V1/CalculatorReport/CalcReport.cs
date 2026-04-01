using System.Collections.Generic;
using System.Linq;

namespace Express.Logic.Kalkulatory.LTR.CalculatorReport
{
    public class CalcReport
    {
        #region <ctor>
        public CalcReport()
        {
            Items = new List<CalcReportItem>();
        }
        #endregion

        #region <props>
        public List<CalcReportItem> Items { get; private set; }
        #endregion

        #region <public methods>

        public void AddItem(CalcReportItem item)
        {
            Items.Add(item);
        }

        public void AddError(string error)
        {
            
            Items.Add(new CalcReportItem()
            {
                Value = error,
                TypeOfItem = CalcReportItemType.Error
            });

        }

        public bool MoveItemAfterItem(string itemToMoveUniqueName, string itemToInsertAfterUniqueName)
        {

            CalcReportItem itemToMove = Items.FirstOrDefault(i => i.UniqueName == itemToMoveUniqueName);
            if (null != itemToMove)
            {
                
                CalcReportItem itemToInsertAfter = Items.FirstOrDefault(i => i.UniqueName == itemToInsertAfterUniqueName);
                if (null != itemToInsertAfter)
                {

                    Items.Remove(itemToMove);

                    int insertingIndex = Items.IndexOf(itemToInsertAfter) + 1;
                    if (insertingIndex < Items.Count)
                    {
                        Items.Insert(insertingIndex, itemToMove);
                        return true;
                    }
                    else if (insertingIndex == Items.Count)
                    {
                        Items.Add(itemToMove);
                        return true;
                    }

                }

            }

            return false;

        }
        #endregion
    }

}
