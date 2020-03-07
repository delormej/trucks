using System;
using System.Collections.Generic;

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
}