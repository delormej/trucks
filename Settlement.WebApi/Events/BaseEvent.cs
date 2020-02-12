using System;
using Settlement.WebApi.Models;

namespace Settlement.WebApi.Events
{
    public abstract class BaseEvent
    {
        public Guid CorrelationId { get; set; }
        public SettlementHeader Header { get; set; }
    }
}