using System;
using System.Collections.Generic;

namespace Trucks
{
    public class SettlementWorkbook : ExcelWorkbook
    {
        private const int MaxRows = 26;
        private const int lastLoadRow = 6;
        private Dictionary<string, string> columns;

        public SettlementWorkbook(string templateFile)
        {
            columns = GetSheetColumns();
            this.Open(templateFile);
        }
        
        public void AddSheet(int week, int truck, string driver)
        {}

        public void AddCredits(IEnumerable<Credit> credits)
        {}

        public void AddDeductions(IEnumerable<Deduction> deductions)
        {}

        public void Save(string filename)
        {}

        private void AddLoadRow(Credit credit)
        {
            if (lastLoadRow >= MaxRows)
                throw new ApplicationException($"Error, cannot exceed {MaxRows} loads per settlement week.");
            
            // string sheetName = GetSheetname(detail.Week);
            // UpdateCellValue(sheetName, GetAddressname("Load"), detail.Load);
            // UpdateCellValue(sheetName, GetAddressname("Miles"), detail.Miles.ToString());
        }

        private void SetDriver(string driver)
        {
            
        }

        private void SetTruck(int truckId)
        {

        }

        private void SetSettlementDate(DateTime date)
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
