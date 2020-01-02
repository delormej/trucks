using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using Azure.Cosmos;
using System.Threading.Tasks;

namespace Trucks
{
    class RevenueReport
    {
        class TruckReport {
            public DateTime SettlementDate;
            public int Year;
            public int WeekNumber;
            public int TruckId;
            public int Miles;
            public double TotalPaid;
            public double TotalDeductions;

            public override string ToString()
            {
                string format = $"{Year}, {WeekNumber}, {SettlementDate.ToString("MM/dd/yyyy")}, {TruckId}, {Miles}, {TotalPaid.ToString("0.00")}, {TotalDeductions.ToString("0.00")}";
                return format;
            }

            public static string Header = "Year, WeekNumber, Date, TruckId, Miles, TotalPaid, TotalDeductions"; 
        }

        private Repository repository;

        public RevenueReport(Repository repository)
        {
            this.repository = repository;
        }

        public async Task GetTruckRevenueGroupBySettlementAsync()
        {
            List<SettlementHistory> settlements = await repository.GetSettlementsAsync();
            IEnumerable<SettlementHistory> orderedSettlements = settlements
                .OrderByDescending(s => s.WeekNumber)
                .OrderByDescending(s => s.Year);
                
            List<TruckReport> reports = new List<TruckReport>();           
            System.Console.WriteLine(TruckReport.Header);

            foreach (var s in orderedSettlements)
            {
                var trucks = s.Credits.GroupBy(c => c.TruckId);
                foreach (var truck in trucks)
                {
                    TruckReport report = new TruckReport() { SettlementDate = s.SettlementDate };
                    report.Year = s.Year;
                    report.WeekNumber = s.WeekNumber;
                    report.TruckId = truck.Key;
                    report.Miles = truck.Sum(t => t.Miles);
                    report.TotalPaid = truck.Sum(t => t.TotalPaid);
                    report.TotalDeductions = s.Deductions.Where(d => d.TruckId == truck.Key).Sum(d => d.TotalDeductions);
                    reports.Add(report);
                    System.Console.WriteLine(report);
                }
            }
        }
    }
}