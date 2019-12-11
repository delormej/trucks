using System;
using System.Collections.Generic;

namespace Trucks
{
    public class SettlementHistory
    {
        public SettlementHistory() {}

        public string SettlementId { get; set; }
        public DateTime SettlementDate { get; set; }

        public List<Credit> Credits { get; set; }
        public List<Deduction> Deductions { get; set; }
    }
}