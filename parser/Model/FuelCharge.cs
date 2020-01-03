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
                return $"{TruckId?.Trim()}-{TransactionDate?.Trim()}-{TransactionTime?.Trim()}"; 
            } 
            set { _id = value; }
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