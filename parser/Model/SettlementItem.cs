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

        public virtual string id { get; set; }
        public virtual string SettlementId { get; set; }    
	    
        public bool Deleted { get; set; }
	    public string LastModifiedBy { get; set; }
	    public DateTime LastModifiedTime { get; set; }
    }
}