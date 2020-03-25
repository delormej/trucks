using System;
using System.Collections.Generic;
using System.Linq;

namespace Trucks
{
    public class SettlementHistory : SettlementItem
    {
        private DateTime settlementDate;
        public SettlementHistory() {}

        // Required for cosmosdb
        public override string id => SettlementId;
        public DateTime DownloadedTimestamp { get; set; }
        public DateTime ConvertedTimestamp { get; set; }

        public DateTime SettlementDate 
        { 
            get { return this.settlementDate; } 
            set 
            {
                this.settlementDate = value;
                int week, year;
                Tools.GetWeekNumber(this.settlementDate, out week, out year);
                this.WeekNumber = week;
                this.Year = year;
            }
        }
        public int WeekNumber { get; private set; }
        public int Year { get; private set; }
        public int CompanyId { get; set; } 
        public double CheckAmount { get; set; }
        public double ARAmount { get; set; }
        public double DeductionAmount { get; set; }
        public List<Credit> Credits { get; set; }
        public List<Deduction> Deductions { get; set; }
    }

    public static class SettlementHistoryExtensions
    {
        public static SettlementHistory FindSettlementById(
            this List<SettlementHistory> settlements, string id)
        {
            return settlements.Find(s => s.SettlementId == id);
        }

        public static bool ContainsCompany(
            this List<SettlementHistory> settlements, int companyId)
        {
            if (settlements == null)
                return false;
            return settlements.Count(s => s.CompanyId == companyId) > 0;
        }
 
         public static IEnumerable<SettlementHistory> FilterSettlementsByTruck(
                this IEnumerable<SettlementHistory> settlements, int truckid)
        {
            return settlements.Where(
                        s => s.Credits.Where(c => c.TruckId == truckid).Count() > 0
                    );
        }

        /// <summary>
        /// Gets the list of drivers from the settlements, optionally for a given truck.
        /// </summary>
        public static IEnumerable<string> GetDrivers(this List<SettlementHistory> settlements, int? truckid)
        {
            IEnumerable<string> drivers = settlements.SelectMany(s => 
                    s.Credits.Where(c => truckid != null ? c.TruckId == truckid : true)
                    .Select(c => c.Driver)).Distinct();                
            
            return drivers;
        }
    }    
}