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
            SettlementWorkbook workbook = new SettlementWorkbook("SettlementHistoryTemplate.xlsx");
            string driver = null;

            foreach (int week in weeks)
            {
                List<Credit> credits = new List<Credit>();
                List<Deduction> deductions = new List<Deduction>();

                foreach (SettlementHistory s in _settlements.Where(s => s.WeekNumber == week))
                {
                    credits.AddRange(s.Credits.Where(c => c.TruckId == truck));
                    deductions.AddRange(s.Deductions.Where(d => d.TruckId == truck));
                }

                if (driver == null)
                    driver = GetDriver(credits);
                workbook.AddSheet(week, truck, driver);
                workbook.AddCredits(credits);
                workbook.AddDeductions(deductions);
            }
            
            string outputFile = GetFilename(year, driver);
            workbook.Save(outputFile);

            return outputFile;
        }

        private string GetDriver(IEnumerable<Credit> credits)
        {
            // TODO: Driver can be different for each line... how do we reconcile this??

            string driver = credits.Where(c => c.CreditDescriptions == "FUEL SURCHARGE CREDIT")
                .Select(c => c.Driver).First();
            return driver;
        }

        private string GetFilename(int year, string driver)
        {
            string format = $"{year} ({driver}) Settlement.xlsx";
            return format;
        }
    }
}