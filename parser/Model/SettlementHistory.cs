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
        public DateTime SettlementDate 
        { 
            get { return this.settlementDate; } 
            set 
            {
                this.settlementDate = value;
                this.WeekNumber = Tools.GetWeekNumber(settlementDate);
                this.Year = settlementDate.Year;
            }
        }
        public int WeekNumber { get; set; }
        public int Year { get; set; }
        public int CompanyId { get; set; } 
        public double CheckAmount { get; set; }
        public double ARAmount { get; set; }
        public double DeductionAmount { get; set; }
        public List<Credit> Credits { get; set; }
        public List<Deduction> Deductions { get; set; }
    }
}