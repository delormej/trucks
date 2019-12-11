using System;

namespace Trucks
{
    public class SettlementItem
    {
        public SettlementItem() {}
        public SettlementItem(string settlementId)
        {
            this.SettlementId = settlementId;
        }

        public string SettlementId { get; set; }    
    }
}