using System;
using System.Collections.Generic;

namespace Trucks
{
    public class DriverSettlement : SettlementItem
    {
        public DriverSettlement() {}

        public string Driver { get; set; }
        public int TruckId { get; set; }
        public int WeekId { get; set; }
        public int Year { get; set; }
        public DateTime DriverSettlementDate { get; set; }
        public double FuelCharges { get; set; }
        public IEnumerable<Credit> Credits { get; set; }
        public IEnumerable<Deduction> Deductions { get; set; }        
    }
}