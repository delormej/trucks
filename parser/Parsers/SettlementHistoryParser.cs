using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace Trucks
{
    public class SettlementHistoryParser
    {
        private string _filename;
        private string _settlementId;

        public static SettlementHistory Parse(string filename)
        {
            SettlementHistoryParser parser = new SettlementHistoryParser(
                filename, GetSettlementIdFromFile(filename));
            SettlementHistory settlement = parser.Parse();         
            
            return settlement;
        }

        public static List<SettlementHistory> ParseLocalFiles(string company)
        {
            int companyId = int.Parse(company);
            List<SettlementHistory> settlements = new List<SettlementHistory>();
            string[] settlementFiles = Directory.GetFiles(company, "*.xlsx");           

            foreach (var filename in settlementFiles)
            {
                SettlementHistory settlement = Parse(filename);
                settlement.CompanyId = companyId;
                settlements.Add(settlement);
                System.Console.WriteLine($"Parsed: {filename} with {settlement.Credits.Count} credits.");            
            }

            return settlements;
        }

        public static string GetSettlementIdFromFile(string file)
        {
            string filename = Path.GetFileName(file);
            int i = filename.IndexOf(".xls");
            if (i <= 0)
                throw new ApplicationException($"Unable to get SettlmentId from filename: {file}");

            return filename.Substring(0, i);            
        }

        public SettlementHistoryParser(string filename, string settlementId)
        {
            this._filename = filename;
            this._settlementId = settlementId;
        }

        public SettlementHistory Parse()
        {
            SettlementHistoryWorkbook workbook = new SettlementHistoryWorkbook(_filename);
            
            SettlementHistory settlement = new SettlementHistory();
            settlement.SettlementId = this._settlementId;          
            settlement.Credits = GetCredits(workbook);
            settlement.Deductions = GetDeductions(workbook);
            settlement.SettlementDate = GetLastCreditDate(settlement);
            return settlement;
        }

        private List<Credit> GetCredits(SettlementHistoryWorkbook workbook)
        {
            return GetSettlementItemsFromSheet<Credit>("MASTER SHEET", workbook);
        }

        private List<Deduction> GetDeductions(SettlementHistoryWorkbook workbook)
        {
            return GetSettlementItemsFromSheet<Deduction>("DEDUCTIONS", workbook);
        }         

        private DateTime GetLastCreditDate(SettlementHistory settlement)
        {
            DateTime? creditDate = ConvertDate(
                settlement.Credits.OrderByDescending(s => ConvertDate(s.CreditDate))
                    .FirstOrDefault().CreditDate);

            if (creditDate != null)
                return (DateTime)creditDate;
            else
                return settlement.SettlementDate;

            DateTime? ConvertDate(string date)
            {
                if (!string.IsNullOrEmpty(date))
                    return DateTime.Parse(date);
                else 
                    return null;
            }
        }

        public List<T> GetSettlementItemsFromSheet<T>(string sheetName, SettlementHistoryWorkbook workbook) where T : SettlementItem, new()
        {
            List<T> items = new List<T>();
            SettlementHistoryWorkbook.HelperSheet sheet = workbook[sheetName];
            if (sheet != null)
            {
                Dictionary<string, PropertyInfo> columnProperties = GetColumnProperties<T>(sheet);
                int rowIndex = 0;

                foreach (SettlementHistoryWorkbook.HelperRow row in sheet.GetRows().Skip(2))
                {
                    if (IsLastRow<T>(row))
                        break;

                    T item = new T();
                    item.SettlementId = _settlementId;
                    item.id = $"{_settlementId}-{rowIndex++}";

                    foreach (SettlementHistoryWorkbook.HelperCell cell in row.GetCells())
                    {
                        if (columnProperties.ContainsKey(cell.Name))
                        {
                            PropertyInfo property = columnProperties[cell.Name];
                            SetValue(property, item, cell);
                        }
                    }
                    items.Add(item);
                }
            }            
            return items;
        }

        private Dictionary<string, PropertyInfo> GetColumnProperties<T>(SettlementHistoryWorkbook.HelperSheet sheet)
        {
            Dictionary<string, PropertyInfo> columnProperties = new Dictionary<string, PropertyInfo>();

            // Get Header Row
            var headerRow = sheet.GetRows().Skip(1).First();
            if (headerRow == null)
                throw new ApplicationException($"Unable to get headers for {typeof(T)}");
            
            foreach (var cell in headerRow.GetCells())
            {
                PropertyInfo property = GetPropertyByHeader<T>(cell.Value);
                if (property != null)
                    columnProperties.Add(cell.Name, property);
            }

            return columnProperties;
        }

        private bool IsLastRow<T>(SettlementHistoryWorkbook.HelperRow row)
        {
            if (typeof(T) == typeof(Deduction))
            {
                var cell = row.GetCells().Where(c => c.Name == "A").FirstOrDefault();
                if (cell == null || string.IsNullOrEmpty(cell.Value))
                    return true;
            }

            if (typeof(T) == typeof(Credit))
            {
                var cell = row.GetCells().Where(c => c.Name == "C").FirstOrDefault();
                if (cell == null || string.IsNullOrEmpty(cell.Value))
                    return true;
            }            
            return false;
        }

        private void SetValue(PropertyInfo property, 
                SettlementItem item,
                SettlementHistoryWorkbook.HelperCell cell)
        {
            if (property != null && !string.IsNullOrWhiteSpace(cell.Value))
            {
                if (property.PropertyType == typeof(int))
                {
                    property.SetValue(item, int.Parse(cell.Value));
                }
                else if (property.PropertyType == typeof(double))
                {
                    property.SetValue(item, double.Parse(cell.Value));
                }
                else
                {
                    property.SetValue(item, cell.Value);
                }
            }            
        }    

        private PropertyInfo GetPropertyByHeader<T>(string header)
        {
            PropertyInfo[] props = typeof(T).GetProperties();
            foreach (PropertyInfo prop in props)
            {
                SheetColumnAttribute attrib = prop.GetCustomAttribute(
                    typeof(SheetColumnAttribute)) as SheetColumnAttribute;

                if (attrib != null && attrib.Header == header)
                {
                    return prop;
                }
            }
            return null;
        }                
    }
}