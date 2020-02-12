using System;

namespace Settlement.WebApi.Events
{
    public abstract class ProcessedSettlementEvent : BaseEvent
    {
        public DateTime ProcessedDate { get; set; }
    }
}