using System;
using Newtonsoft.Json;

namespace Trucks
{
    public class FuelCharge
    {
        private string _id;

        public FuelCharge() {}

        public virtual string id 
        { 
            get 
            { 
                return $"{TruckId?.Trim()}-{TransactionDate?.Trim()}-{TransactionTime?.Trim()}"
                    .Replace(" ", "");
            } 
            set { _id = value; }
        }

        public int Week 
        {
            get 
            {
                if (TransactionDate != null)
                {
                    int week, year;
                    Tools.GetWeekNumber(DateTime.Parse(TransactionDate), out week, out year);
                    return week;
                }
                else
                {
                    return default(int);
                }
            }
            set { Week = value; } // Really shouldn't have a set implementation, but seems to need to be here for serialization to work.
        }

        [JsonProperty("Transaction_Date")]
        public string TransactionDate { get; set; }
        
        [JsonProperty("Transaction_Time")]
        public string TransactionTime { get; set; }

        [JsonProperty("Net_Cost")]
        public double NetCost { get; set; }

        [JsonProperty("Emboss_Line_2")]
        public string TruckId { get; set; }

        public override string ToString()
        {
            return $"{id}, {TruckId}, {TransactionDate}, {NetCost}";
        }
    }
}