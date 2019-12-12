using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using Doc = DocumentFormat.OpenXml.Spreadsheet;

namespace Trucks
{
    public class SettlementHistoryWorkbook : ExcelWorkbook
    {
        public SettlementHistoryWorkbook(string filename)
        {
            this.Open(filename);
        }
       
        public HelperSheet this[string sheetName]
        {
            get 
            {
                return GetSheetByName(sheetName);
            }
        }

        private HelperSheet GetSheetByName(string sheetName)
        {
            var sheets = wbPart.Workbook.Descendants<Doc.Sheet>();
            Doc.Sheet sheet = sheets.Where(s => s.Name == sheetName).FirstOrDefault();
            if (sheet == null)
                throw new ApplicationException($"Cannot find a sheet named {sheetName}");
            WorksheetPart worksheetPart = (WorksheetPart)document.WorkbookPart.GetPartById(sheet.Id);
            Doc.Worksheet worksheet = worksheetPart.Worksheet;    
            return new HelperSheet(this, worksheet, sheetName);
        }

        private static string GetColumnName(string cellReference)
        {
            if (ColumnNameRegex.IsMatch(cellReference))
                return ColumnNameRegex.Match(cellReference).Value;

            throw new ArgumentOutOfRangeException(cellReference);
        }

        private static readonly Regex ColumnNameRegex = new Regex("[A-Za-z]+");

        public class HelperSheet
        {
            internal Doc.Worksheet _worksheet;
            internal SettlementHistoryWorkbook _parent;

            public HelperSheet(SettlementHistoryWorkbook parent, Doc.Worksheet worksheet, string sheetName)
            {
                _parent = parent;
                _worksheet = worksheet;
                this.SheetName = sheetName;
                var rows = worksheet.GetFirstChild<Doc.SheetData>().Elements<Doc.Row>();
            }
            
            public string SheetName { get; set; }
            
            public IEnumerable<HelperRow> GetRows() 
            {
                foreach (var row in _worksheet.GetFirstChild<Doc.SheetData>().Elements<Doc.Row>())
                    yield return new HelperRow(_parent, SheetName, row);
            }
        }
        public class HelperRow
        {
            Doc.Row _row;
            SettlementHistoryWorkbook _parent;
            string _sheetName;

            internal HelperRow(SettlementHistoryWorkbook parent, string sheetName, Doc.Row row)
            {
                _parent = parent;
                _row = row;
                _sheetName = sheetName;
            }
            
            public IEnumerable<HelperCell> GetCells()
            {
                foreach (var cell in _row.Elements<Doc.Cell>())
                    yield return new HelperCell(_parent, _sheetName, cell);
            }
        }
        public class HelperCell
        {
            private Doc.Cell _cell;
            SettlementHistoryWorkbook _parent;
            string _sheetName;

            internal HelperCell(SettlementHistoryWorkbook parent, string sheetName, Doc.Cell cell)
            {
                _cell = cell;
                _parent = parent;
                _sheetName = sheetName;
            }

            public string Name 
            { 
                get { return GetColumnName(_cell.CellReference); }
            }

            public string Value 
            {
                get { return _parent.GetCellValue(_sheetName, _cell.CellReference); }
            }
        }
    }
}