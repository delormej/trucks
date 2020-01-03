using System;
using System.Collections.Generic;

namespace Trucks
{
    public class SettlementWorkbook : ExcelWorkbook
    {
        private static readonly Dictionary<string, string> _columns = GetSheetColumns();
        private const int MaxRows = 26;
        private int _lastLoadRow = 5;
        private string _outputFilename = null;
        string _sheetName = null;
        int _truck = 0;
        string _driver = null;
        DateTime _settlementDate;

        public string TemplateFile = "Excel/1-Settlement Template 50-50 DET SOLO.xlsx";

        public SettlementWorkbook(int year, int truck, string driver, DateTime settlementDate)
        {
            _truck = truck;
            _driver = driver;
            _settlementDate = settlementDate;
            _outputFilename = GetFilename(year, driver);
        }

        public string Create()
        {            
            System.IO.File.Copy(TemplateFile, _outputFilename, true);
            this.Open(_outputFilename);
            return _outputFilename;                    
        }

        public void AddSheet(int week, IEnumerable<Credit> credits, IEnumerable<Deduction> deductions)
        {
            _sheetName = GetSheetname(week);
             _lastLoadRow = 5;

            SetTruck();
            SetDriver();    
            SetSettlementDate();
            AddCredits(credits);
        }

        public void Save()
        {
            this.document.Save();
        }

        private string GetFilename(int year, string driver)
        {
            string format = $"{year} ({driver}) Settlement.xlsx";
            return format;
        }

        private void AddCredits(IEnumerable<Credit> credits)
        {
            foreach (var c in credits)
            {
                if (++_lastLoadRow >= MaxRows)
                    throw new ApplicationException($"Error, cannot exceed {MaxRows} loads per settlement week.");

                if (c.ProNumber !=null && c.ProNumber.Length > 6)
                    UpdateCellValue("Load", c.ProNumber.Substring(c.ProNumber.Length - 5));
                UpdateCellValue("Miles", c.Miles);
                UpdateCellValue("Rev", c.ExtendedAmount);
                UpdateCellValue("FSC", c.CreditAmount);
                UpdateCellValue("Advance", c.AdvanceAmount);
                UpdateCellValue("DH", c.DeadHead);
                UpdateCellValue("EM", c.Empty);
                UpdateCellValue("Tolls", c.Tolls);
                UpdateCellValue("Other", c.Other);
                UpdateCellValue("CBC", c.Canada);
                UpdateCellValue("Stops", c.StopOff);
                UpdateCellValue("Detent", c.Detention);
                UpdateCellValue("H load", c.HandLoad);
                UpdateCellValue("Layovr", c.Layover);
                UpdateCellValue("Accessorial Other", c.Other);                                
            }
        }

        private void AddDeductions(IEnumerable<Deduction> deductions)
        {}

        private void SetDriver()
        {
            const string DriverCell = "B1";
            UpdateCellValue(_sheetName, DriverCell, _driver);            
        }

        private void SetTruck()
        {
            const string TruckIdCell = "B2";
            UpdateCellValue(_sheetName, TruckIdCell, _truck.ToString());
        }

        private void SetSettlementDate()
        {
            const string SettlementDateCell = "C3";
            UpdateCellValue(_sheetName, SettlementDateCell, _settlementDate.ToString("yyyy-MM-dd"));
        }
        
        private string GetSheetname(int week)
        {
            return string.Format("Week_{0}", week);
        }

        private string GetAddressname(string column)
        {
            return string.Format("{0}{1}", _columns[column], _lastLoadRow);
        }

        private void UpdateCellValue(string column, int value)
        {
            if (value > 0)
                UpdateCellValue(_sheetName, GetAddressname(column), value.ToString());
        }

        private void UpdateCellValue(string column, double value)
        {
            if (value > 0)
                UpdateCellValue(_sheetName, GetAddressname(column), value.ToString());
        }

        private void UpdateCellValue(string column, string value)
        {
            UpdateCellValue(_sheetName, GetAddressname(column), value);
        }

        private static Dictionary<string, string> GetSheetColumns() 
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

        ~SettlementWorkbook()
        {
            Dispose();
        }

        public override void Dispose()
        {
            try 
            {
                if (document != null)
                {
                    document.Close();
                    document.Dispose();
                }
            }
            catch{/*ignore any errors here*/}
        }                
    }
}
