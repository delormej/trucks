using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Trucks
{
    public class SettlementWorkbookGenerator
    {
        public SettlementWorkbookGenerator()
        {
        }

        public async Task<SettlementWorkbook> GenerateAsync(int year, int[] weeks, int truck)
        {       
            Repository repository = new Repository();    
            List<SettlementHistory> settlements = await repository.GetSettlementsByWeekAsync(year, weeks);
            
            foreach (int week in weeks)
            {
                List<Credit> credits = new List<Credit>();
                List<Deduction> deductions = new List<Deduction>();

                foreach (SettlementHistory s in settlements.Where(s => s.WeekNumber == week))
                {
                    credits.AddRange(s.Credits.Where(c => c.TruckId == truck));
                    deductions.AddRange(s.Deductions.Where(d => d.TruckId == truck));
                }

                foreach(var c in credits)
                    System.Console.WriteLine($"{c.TruckId}, {c.Driver}, {c.TotalPaid}");

                foreach(var d in deductions)
                    System.Console.WriteLine($"{d.TruckId}, {d.Driver}, {d.TotalDeductions}");                    

            }

            // System.Console.WriteLine($"Found {settlements.Count()} settlements");



            return null;
        }

    }
}