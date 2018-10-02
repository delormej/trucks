using System;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Trucks
{
    // TODO:
    // 0. Refactor for class to only have GetCellValue, InsertCellValue, CopySheet methods
    // 1. Refactor and add method for InsertCellValue(sheetName, addressName, value)
    // 2. Copy "Week_1" as template sheet for each subsequent week

    public class ExcelWorkbook : IDisposable
    {
        private SpreadsheetDocument document = null;
        private WorkbookPart wbPart = null;

        public ExcelWorkbook()
        {
        }

        ~ExcelWorkbook()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (document != null)
            {
                document.Close();
                document.Dispose();
            }
        }

        public void Open(string fileName)
        {
            document = SpreadsheetDocument.Open(fileName, true);
            if (document == null)
                throw new ArgumentException("Unable to open file.");
            // Retrieve a reference to the workbook part.
            wbPart = document.WorkbookPart;
        }

        public void Save()
        {
        }

        // Retrieve the value of a cell, given a file name, sheet name, 
        // and address name.
        public string GetCellValue(string sheetName, string addressName)
        {
            if (document == null)
                throw new InvalidOperationException("Document must be open first.");

            string value = string.Empty;
            Cell theCell = GetCell(sheetName, addressName);

            // If the cell does not exist, return an empty string.
            if (theCell != null)
            {
                value = theCell.InnerText;

                // If the cell represents an integer number, you are done. 
                // For dates, this code returns the serialized value that 
                // represents the date. The code handles strings and 
                // Booleans individually. For shared strings, the code 
                // looks up the corresponding value in the shared string 
                // table. For Booleans, the code converts the value into 
                // the words TRUE or FALSE.
                if (theCell.DataType != null)
                {
                    switch (theCell.DataType.Value)
                    {
                        case CellValues.SharedString:

                            // For shared strings, look up the value in the
                            // shared strings table.
                            var stringTable = 
                                wbPart.GetPartsOfType<SharedStringTablePart>()
                                .FirstOrDefault();

                            // If the shared string table is missing, something 
                            // is wrong. Return the index that is in
                            // the cell. Otherwise, look up the correct text in 
                            // the table.
                            if (stringTable != null)
                            {
                                value = 
                                    stringTable.SharedStringTable
                                    .ElementAt(int.Parse(value)).InnerText;
                            }
                            break;

                        case CellValues.Boolean:
                            switch (value)
                            {
                                case "0":
                                    value = "FALSE";
                                    break;
                                default:
                                    value = "TRUE";
                                    break;
                            }
                            break;
                    }
                }
            }
            return value;
        }        

        public void UpdateCellValue(string sheetName, string addressName, string value)
        {
            /*
            Cell cell = new Cell()   
                { CellReference = "A1", DataType = CellValues.String,  
                    CellValue = new CellValue("Microsoft") };  
             */
            Cell theCell = GetCell(sheetName, addressName);
            if (theCell == null)
            {
                // Only supports updating the value of an existing cell, if it doesn't exist - throw an error.
                // Here's how to add a new cell if it doesn't exist:
                // https://docs.microsoft.com/en-us/office/open-xml/how-to-insert-text-into-a-cell-in-a-spreadsheet#sample-code
                throw new ArgumentException("Cell address does not refer to an existing cell in the workbook.");
            }
            return;
            theCell.CellValue = new CellValue(value);
        }

        private Cell GetCell(string sheetName, string addressName)
        {
            // Find the sheet with the supplied name, and then use that 
            // Sheet object to retrieve a reference to the first worksheet.
            Sheet theSheet = wbPart.Workbook.Descendants<Sheet>().
                Where(s => s.Name == sheetName).FirstOrDefault();

            // Throw an exception if there is no sheet.
            if (theSheet == null)
            {
                throw new ArgumentException("sheetName");
            }

            // Retrieve a reference to the worksheet part.
            WorksheetPart wsPart = 
                (WorksheetPart)(wbPart.GetPartById(theSheet.Id));

            // Use its Worksheet property to get a reference to the cell 
            // whose address matches the address you supplied.
            Cell theCell = wsPart.Worksheet.Descendants<Cell>().
                Where(c => c.CellReference == addressName).FirstOrDefault();

//theCell.CellValue = new CellValue("Rob De Lorme(2)");
//theCell.DataType = new EnumValue<CellValues>(CellValues.String);
theCell.DataType = CellValues.InlineString;
theCell.InlineString = new InlineString() { Text = new Text("Robert") };
wsPart.Worksheet.Save();
            return theCell;        
        }
    }
}
