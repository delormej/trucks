using System;

namespace Settlement.WebApi.Events
{
    public abstract class ConvertedSettlementEvent : BaseEvent
    {
        public DateTime ConvertedDate { get; set; }
        public string SettlementXlsxUrl { get; set; }
    }
}