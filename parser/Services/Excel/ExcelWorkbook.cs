using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using DocumentFormat.OpenXml;
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
        protected SpreadsheetDocument document = null;
        protected WorkbookPart wbPart = null;

        public ExcelWorkbook()
        {
        }

        ~ExcelWorkbook()
        {
            Dispose();
        }

        public virtual void Dispose()
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
            this.document.Save();
        }

        // Retrieve the value of a cell, given a file name, sheet name, 
        // and address name.
        public string GetCellValue(string sheetName, string addressName)
        {
            if (document == null)
                throw new InvalidOperationException("Document must be open first.");

            string value = string.Empty;
            Cell theCell = GetCell(GetWorksheetPart(sheetName), addressName);

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
            WorksheetPart wsPart = GetWorksheetPart(sheetName);
            Cell theCell = GetCell(wsPart, addressName);
            if (theCell == null)
            {
                // Only supports updating the value of an existing cell, if it doesn't exist - throw an error.
                // Here's how to add a new cell if it doesn't exist:
                // https://docs.microsoft.com/en-us/office/open-xml/how-to-insert-text-into-a-cell-in-a-spreadsheet#sample-code
                throw new ArgumentException("Cell address does not refer to an existing cell in the workbook.");
            }

            double number = 0;
            if (double.TryParse(value, out number))
            {
                theCell.DataType = CellValues.Number;
                theCell.CellValue = new CellValue(value);
                theCell.InlineString = null;
            }
            else
            {
                if (theCell.CellFormula != null)
                    DeleteCellFormula(theCell, wsPart);

                theCell.DataType = CellValues.InlineString;
                theCell.CellValue = null; // new CellValue(value);
                theCell.InlineString = new InlineString() { Text = new Text(value) };
            }
            
            wbPart.Workbook.CalculationProperties.ForceFullCalculation = true;
            wbPart.Workbook.CalculationProperties.FullCalculationOnLoad = true;
            wsPart.Worksheet.Save();
        }

        public void UpdateCellFormula(string sheetName, string addressName, string formula)
        {
            WorksheetPart wsPart = GetWorksheetPart(sheetName);
            Cell theCell = GetCell(wsPart, addressName);
            if (theCell == null)
            {
                // Only supports updating the value of an existing cell, if it doesn't exist - throw an error.
                // Here's how to add a new cell if it doesn't exist:
                // https://docs.microsoft.com/en-us/office/open-xml/how-to-insert-text-into-a-cell-in-a-spreadsheet#sample-code
                throw new ArgumentException("Cell address does not refer to an existing cell in the workbook.");
            }

            theCell.DataType = new EnumValue<CellValues>(CellValues.Number);
            theCell.CellFormula = new CellFormula(formula);
            wsPart.Worksheet.Save();           

            wbPart.Workbook.CalculationProperties.ForceFullCalculation = true;
            wbPart.Workbook.CalculationProperties.FullCalculationOnLoad = true;       
        }

        private void DeleteCellFormula(Cell cell, WorksheetPart wsPart)
        {
            try
            {
                CalculationChainPart calculationChainPart = wbPart.CalculationChainPart;
                CalculationChain calculationChain = calculationChainPart.CalculationChain;
                var calculationCells = calculationChain.Elements<CalculationCell>().ToList();

                if (cell.CellFormula != null && cell.CellValue != null)
                {
                    string cellRef = cell.CellReference;                            
                    CalculationCell calculationCell = calculationCells.Where(c => c.CellReference == cellRef).FirstOrDefault();

                    cell.CellFormula.Remove();
                    if(calculationCell != null)
                    {                       
                        calculationCell.Remove();
                        calculationCells.Remove(calculationCell);
                    }
                    else
                    {
                        System.Console.WriteLine("Unable to delete cell formula, no further details.");
                    }                   
                }
                if (calculationCells.Count == 0)
                    wbPart.DeletePart(calculationChainPart);                
            }
            catch (Exception e)
            {
                throw new ApplicationException("Unable to delete cell formula.", e);
            }
        }

        private WorksheetPart GetWorksheetPart(string sheetName)
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
            
            return wsPart;
        }

        private Cell GetCell(WorksheetPart wsPart, string addressName)
        {
            // Use its Worksheet property to get a reference to the cell 
            // whose address matches the address you supplied.
            Cell theCell = wsPart.Worksheet.Descendants<Cell>().
                Where(c => c.CellReference == addressName).FirstOrDefault();

            return theCell;        
        }

        public void CopySheet(string source, string destination)
        { 
            throw new NotImplementedException("This is not working properly...");

            // WorksheetPart sourceSheetPart = GetWorksheetPart(source);
            // SpreadsheetDocument tempSheet = SpreadsheetDocument.Create(new MemoryStream(), document.DocumentType);
            // WorkbookPart tempWorkbookPart = tempSheet.AddWorkbookPart();
            // WorksheetPart tempWorksheetPart = tempWorkbookPart.AddPart<WorksheetPart>(sourceSheetPart);
            // WorksheetPart clonedSheet = wbPart.AddPart<WorksheetPart>(tempWorksheetPart);            

            // Sheets sheets = wbPart.Workbook.GetFirstChild<Sheets>();
            // Sheet copiedSheet = new Sheet();
            // copiedSheet.Name = destination;
            // copiedSheet.Id = wbPart.GetIdOfPart(clonedSheet);
            // copiedSheet.SheetId = (uint)sheets.ChildElements.Count + 1;
            // sheets.Append(copiedSheet);
            // wbPart.Workbook.Save();
        }

        public void DeleteSheet(string sheetname)
        {
            throw new NotImplementedException("This is not working properly...");

            // Sheet theSheet = wbPart.Workbook.Descendants<Sheet>().Where(s => s.Name == sheetname).FirstOrDefault();
            // if (theSheet == null)
            //     throw new ArgumentException($"Sheet {sheetname} not found.");

            // Int32Value Sheetid = (int)theSheet.SheetId.Value;

            // // Remove the sheet reference from the workbook.
            // WorksheetPart worksheetPart = (WorksheetPart)(wbPart.GetPartById(theSheet.Id));
            // theSheet.Remove();

            // // Delete the worksheet part.
            // wbPart.DeletePart(worksheetPart);

            // //Get the DefinedNames
            // var definedNames = wbPart.Workbook.Descendants<DefinedNames>().FirstOrDefault();
            // if (definedNames != null)
            // {
            //     List<DefinedName> defNamesToDelete = new List<DefinedName>();

            //     foreach (DefinedName Item in definedNames)
            //     {
            //         // This condition checks to delete only those names which are part of Sheet in question
            //         if (Item.Text.Contains(sheetname + "!"))
            //             defNamesToDelete.Add(Item);
            //     }

            //     foreach (DefinedName Item in defNamesToDelete)
            //     {
            //         Item.Remove();
            //     }

            // }
            // // Get the CalculationChainPart 
            // //Note: An instance of this part type contains an ordered set of references to all cells in all worksheets in the 
            // //workbook whose value is calculated from any formula

            // CalculationChainPart calChainPart;
            // calChainPart = wbPart.CalculationChainPart;
            // if (calChainPart != null)
            // {
            //     var calChainEntries = calChainPart.CalculationChain.Descendants<CalculationCell>().Where(c => c.SheetId == Sheetid);
            //     List<CalculationCell> calcsToDelete = new List<CalculationCell>();
            //     foreach (CalculationCell Item in calChainEntries)
            //     {
            //         calcsToDelete.Add(Item);
            //     }

            //     foreach (CalculationCell Item in calcsToDelete)
            //     {
            //         Item.Remove();
            //     }

            //     if (calChainPart.CalculationChain.Count() == 0)
            //     {
            //         wbPart.DeletePart(calChainPart);
            //     }
            // }

            // // Save the workbook.
            // wbPart.Workbook.Save();            
        }
    }
}
