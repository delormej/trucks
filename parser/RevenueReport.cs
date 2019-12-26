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
            public int TruckId;
            public int Miles;
            public double TotalPaid;
            public double TotalDeductions;

            public override string ToString()
            {
                string format = $"{SettlementDate.ToString("MM/dd/yyyy")}, {TruckId}, {Miles}, {TotalPaid.ToString("0.00")}, {TotalDeductions.ToString("0.00")}";
                return format;
            }
        }

        private Repository repository;

        public RevenueReport(Repository repository)
        {
            this.repository = repository;
        }

        public async Task GetTruckRevenueGroupBySettlementAsync()
        {
            List<SettlementHistory> settlements = await repository.GetSettlementsAsync();
            List<TruckReport> reports = new List<TruckReport>();
            
            foreach (var s in settlements)
            {
                var trucks = s.Credits.GroupBy(c => c.TruckId);
                foreach (var truck in trucks)
                {
                    TruckReport report = new TruckReport() { SettlementDate = s.SettlementDate };
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