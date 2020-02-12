using System;

namespace Settlement.WebApi.Events
{
    public abstract class NewSettlementEvent : BaseEvent
    {
        public DateTime DownloadDate { get; set; }
        public string SettlementXlsUrl { get; set; }
    }
}