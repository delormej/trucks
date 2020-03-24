using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace Trucks
{
    /// <summary>
    /// Helper to parse an excel spreadsheet into a Settlement object.
    /// </summary>
    public class SettlementHistoryParser
    {
        private string _filename;
        private SettlementHistory _settlement;

        public static SettlementHistory Parse(string filename)
        {
            SettlementHistoryParser parser = new SettlementHistoryParser(filename);
            SettlementHistory settlement = parser.Parse();         
            
            return settlement;
        }

        public static List<SettlementHistory> ParseLocalFiles(string directory)
        {
            List<SettlementHistory> settlements = new List<SettlementHistory>();
            string[] settlementFiles = Directory.GetFiles(directory, "*.xlsx");           

            foreach (var filename in settlementFiles)
            {
                if (filename.Contains("~$"))
                {
                    System.Console.WriteLine($"Skipping temp file {filename}.");
                    continue;
                }

                try
                {
                    SettlementHistory settlement = Parse(filename);
                    if (settlement == null)
                    {
                        System.Console.WriteLine($"Unable to read {filename}");
                        continue;
                    }
                    settlements.Add(settlement);
                    System.Console.WriteLine($"Parsed: {filename} with {settlement.Credits.Count} credits.");            
                }
                catch (Exception e)
                {
                    System.Console.WriteLine($"Unable to parse {filename}, error:\n{e}");
                }
            }

            return settlements;
        }

        private SettlementHistoryParser(string filename)
        {
            this._filename = filename;
        }

        private SettlementHistory Parse()
        {
            try
            {
                SettlementFile file = SettlementFile.FromFilename(_filename);
                if (string.IsNullOrWhiteSpace(file.SettlementId))
                    throw new ApplicationException($"Unable to find settlement idea for {_filename}.");

                _settlement = new SettlementHistory();
                _settlement.SettlementId = file.SettlementId;
                _settlement.CompanyId = file.CompanyId;

                SettlementHistoryWorkbook workbook = new SettlementHistoryWorkbook(_filename);
                _settlement.Credits = GetCredits(workbook);
                _settlement.Deductions = GetDeductions(workbook);
                _settlement.SettlementDate = GetLastCreditDate();
                return _settlement;
            }
            catch (Exception e)
            {
                System.Console.WriteLine($"Error parsing {_filename}: Error:\n\t{e.Message}");
                return null;
            }
        }

        private List<Credit> GetCredits(SettlementHistoryWorkbook workbook)
        {
            return GetSettlementItemsFromSheet<Credit>("MASTER SHEET", workbook);
        }

        private List<Deduction> GetDeductions(SettlementHistoryWorkbook workbook)
        {
            return GetSettlementItemsFromSheet<Deduction>("DEDUCTIONS", workbook);
        }         

        private DateTime GetLastCreditDate()
        {
            DateTime? creditDate = ConvertDate(
                _settlement.Credits.OrderByDescending(s => ConvertDate(s.CreditDate))
                    .FirstOrDefault().CreditDate);

            if (creditDate != null)
                return (DateTime)creditDate;
            else
                return _settlement.SettlementDate;

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
                    item.SettlementId = _settlement.SettlementId;
                    item.id = $"{_settlement.SettlementId}-{rowIndex++}";

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
            try 
            {
                if (property != null && !string.IsNullOrWhiteSpace(cell.Value))
                {
                    if (property.PropertyType == typeof(int))
                    {
                        int value = 0;
                        if (int.TryParse(cell.Value, out value))
                            property.SetValue(item, value);
                        else
                            System.Console.WriteLine($"WARNING: Unable to set value {item} on {property.Name} for {cell.Value}");
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
            catch (Exception e)        
            {
                System.Console.WriteLine($"Unable to set value {item} on {property.Name} for {cell.Value}\n\t{e.Message}");
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