using System;
using System.Collections.Generic;

namespace Trucks
{
    public class SettlementWorkbook : ExcelWorkbook
    {
        private int lastLoadRow;
        private Dictionary<string, string> columns;

        public SettlementWorkbook()
        {
            lastLoadRow = 6; // first row for data.
            columns = GetSheetColumns();
        }
        
        public void AddLoadRow(RevenueDetail detail)
        {
            if (lastLoadRow >= 26)
                throw new ApplicationException("Error, cannot exceed 20 loads per settlement week.");
            
            string sheetName = GetSheetname(detail.Week);
            UpdateCellValue(sheetName, GetAddressname("Load"), detail.Load);
            UpdateCellValue(sheetName, GetAddressname("Miles"), detail.Miles.ToString());
        }

        public void SetDriver(Driver driver)
        {

        }

        public void SetTruck(string truck)
        {

        }

        public void SetSettlementDate(DateTime date)
        {

        }
        
        private string GetSheetname(int week)
        {
            return string.Format("Week_{0}", week);
        }

        private string GetAddressname(string column)
        {
            return string.Format("{0}{1}", columns[column], lastLoadRow);
        }

        private Dictionary<string, string> GetSheetColumns() 
        {
            var list = new Dictionary<string, string>();
            list.Add("Load", "A");
            list.Add("Miles", "B");
            list.Add("Rev", "C");
            list.Add("FSC", "E");
            list.Add("Advance", "F");
            list.Add("DH", "H");
            list.Add("EM", "I");
            list.Add("Tolls", "J");
            list.Add("Other", "K");
            list.Add("CBC", "N");
            list.Add("Stops", "O");
            list.Add("Detent", "P");
            list.Add("H load", "Q");
            list.Add("Layovr", "R");
            list.Add("Accessorial Other", "S");

            return list;
        }
    }
}
