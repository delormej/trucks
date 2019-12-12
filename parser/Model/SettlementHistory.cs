using System;
using System.Collections.Generic;

namespace Trucks
{
    public class SettlementHistory
    {
        public SettlementHistory() {}

        // Required for cosmosdb
        public string id => SettlementId;
        
        public string SettlementId { get; set; } 
        
        public DateTime SettlementDate { get; set; }
        // all trucks: 33357, 44510 
        // 49809 only trucks: 33451, 33438, 33446, 33425, 33426 
        public int CompanyId { get; set; } 
        public List<Credit> Credits { get; set; }
        public List<Deduction> Deductions { get; set; }
    }
}