using System;

namespace Settlement.WebApi.Models
{
    public class SettlementHeader
    {        
        public string CompanyId { get; set; }
        public string SettlementId { get; set; }
        public DateTime SettlementDate { get; set; }
    }
}
