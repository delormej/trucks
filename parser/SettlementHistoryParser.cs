using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace Trucks
{
    public class SettlementHistoryParser
    {
        private string _filename;
        private string _settlementId;
        private DateTime _date;

        public SettlementHistoryParser(string filename, string settlementId, DateTime date)
        {
            this._filename = filename;
            this._settlementId = settlementId;
            this._date = date;
        }

        public SettlementHistory Parse()
        {
            SettlementHistoryWorkbook workbook = new SettlementHistoryWorkbook(_filename);
            
            SettlementHistory settlement = new SettlementHistory();
            settlement.SettlementId = this._settlementId;          
            settlement.SettlementDate = this._date;
            settlement.Credits = GetCredits(workbook);
            settlement.Deductions = GetDeductions(workbook);
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

        public List<T> GetSettlementItemsFromSheet<T>(string sheetName, SettlementHistoryWorkbook workbook) where T : SettlementItem, new()
        {
            List<T> items = new List<T>();
            SettlementHistoryWorkbook.HelperSheet sheet = workbook[sheetName];
            if (sheet != null)
            {                        
                foreach (SettlementHistoryWorkbook.HelperRow row in sheet.GetRows().Skip(2))
                {
                    if (IsLastRow<T>(row))
                        break;

                    T item = new T();
                    item.SettlementId = _settlementId;
                    foreach (SettlementHistoryWorkbook.HelperCell cell in row.GetCells())
                    {
                        SetValue<T>(item, cell);
                    }
                    items.Add(item);
                }
            }            
            return items;
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

        private void SetValue<T>(T item, SettlementHistoryWorkbook.HelperCell cell)
        {
            PropertyInfo property = GetPropertyByColumn<T>(cell.Name);
            if (property != null)
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

        private PropertyInfo GetPropertyByColumn<T>(string column)
        {
            PropertyInfo[] props = typeof(T).GetProperties();
            foreach (PropertyInfo prop in props)
            {
                SheetColumnAttribute attrib = prop.GetCustomAttribute(typeof(SheetColumnAttribute)) as SheetColumnAttribute;
                if (attrib != null && attrib.Column == column)
                {
                    return prop;
                }
            }
            return null;
        }        
    }

    // internal class Headers<T> where T : SettlementItem
    // {
    //     const int HEADER_ROW = 2; // 0 based index.

    //     public Headers(Sheet sheet)
    //     {

    //     }

    //     public PropertyInfo this[string column]
    //     {
    //         get { return GetPropertyByColumn(column); }
    //     }

    //     private static PropertyInfo GetPropertyByColumn(string column)
    //     {
    //         PropertyInfo[] props = typeof(Credit).GetProperties();
    //         foreach (PropertyInfo prop in props)
    //         {
    //             SheetColumnAttribute attrib = prop.GetCustomAttribute(typeof(SheetColumnAttribute)) as SheetColumnAttribute;
    //             if (attrib != null && attrib.Column == column)
    //             {
    //                 return prop;
    //             }
    //         }
    //         return null;
    //     }
    // }    
}