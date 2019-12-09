using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Trucks
{
    public class SettlementHistoryWorkbook : ExcelWorkbook
    {
        private static string GetColumnName(string cellReference)
        {
            if (ColumnNameRegex.IsMatch(cellReference))
                return ColumnNameRegex.Match(cellReference).Value;

            throw new ArgumentOutOfRangeException(cellReference);
        }

        private static readonly Regex ColumnNameRegex = new Regex("[A-Za-z]+");

        public string GetValue(string filename)
        {
            Open(filename);
            var sheets = wbPart.Workbook.Descendants<Sheet>();

            foreach (Sheet sheet in sheets)
            {
                System.Console.WriteLine($"Sheet: {sheet.Name}");
                WorksheetPart worksheetPart = (WorksheetPart)document.WorkbookPart.GetPartById(sheet.Id);
                Worksheet worksheet = worksheetPart.Worksheet;
                var rows = worksheet.GetFirstChild<SheetData>().Elements<Row>();
                foreach (var row in rows)
                {
                    var cells = row.Elements<Cell>();
                    foreach (var cell in cells)
                    {
                        if(GetColumnName(cell.CellReference) == "A")
                        {
                            //var str = cell.CellValue.Text;
                            string str = GetCellValue(sheet.Name, cell.CellReference);
                            System.Console.WriteLine(str);
                            // do whatewer you want
                        }
                    }
                }
            }
            return "";
        }
    }
}