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

        /// <summary>
        /// Creates a new Guid for Id, needs to be unique.
        /// </summary>
        public string id { get; set; }

        public string SettlementId { get; set; }    
    }
}