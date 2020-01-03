using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Trucks
{
    public class SettlementWorkbookGenerator
    {
        private List<SettlementHistory> _settlements;

        public SettlementWorkbookGenerator(List<SettlementHistory> settlements)
        {
            this._settlements = settlements;
        }

        public string Generate(int year, int[] weeks, int truck)
        {
            string outputFile = null;
            SettlementWorkbook workbook = null; 
            
            try
            {
                foreach (int week in weeks)
                {
                    List<Credit> credits = new List<Credit>();
                    List<Deduction> deductions = new List<Deduction>();
                    DateTime settlementDate = _settlements.Where(
                        s => s.WeekNumber == week 
                        && s.Credits.Where(c => c.TruckId == truck).Count() > 0
                    ).First().SettlementDate;

                    foreach (SettlementHistory s in _settlements.Where(s => s.WeekNumber == week))
                    {
                        credits.AddRange(s.Credits.Where(c => c.TruckId == truck));
                        deductions.AddRange(s.Deductions.Where(d => d.TruckId == truck));
                    }

                    if (workbook == null)
                    {
                        string driver = GetDriver(credits);
                        if (driver != null)
                        {
                            workbook = new SettlementWorkbook(year, truck, driver, settlementDate);
                            outputFile = workbook.Create();
                        }
                        // TODO: if no driver, these were entries attributable back to corp... how do we handle?
                    }

                    if (workbook != null)
                    {
                        workbook.AddSheet(week, credits, deductions);
                        // workbook.DeleteSheet("Week_");
                        workbook.Save();
                    }
                }
            }
            catch (Exception e)
            {
                System.Console.WriteLine($"Error generating workbook {outputFile ?? "null"}\n\t{e.Message}");
            }
            finally
            {
                if (workbook != null)
                    workbook.Dispose();
            }

            return outputFile;
        }

        private string GetDriver(IEnumerable<Credit> credits)
        {
            // TODO: Driver can be different for each line... how do we reconcile this??
            string driver = credits.Where(c => c.CreditDescriptions == "FUEL SURCHARGE CREDIT")
                .Select(c => c.Driver).FirstOrDefault();
            return driver;
        }
    }
}