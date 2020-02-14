using System;
using Newtonsoft.Json;

namespace Trucks
{
    public class FuelCharge
    {
        private string _id;
        private string _transactionDate;
        
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

        public int WeekNumber 
        {
            get;
            private set; 
        }

        public int Year 
        {
            get;
            private set;
        }        

        [JsonProperty("Transaction_Date")]
        public string TransactionDate
        {
            get { return _transactionDate; }
            set
            {
                _transactionDate = value;
                int week, year;
                Tools.GetWeekNumber(DateTime.Parse(_transactionDate), out week, out year);
                if (WeekNumber == 52)
                    year++;
                this.WeekNumber = (week+1)%52;
                this.Year = year;
            }
        }
        
        [JsonProperty("Transaction_Time")]
        public string TransactionTime  { get; set; }

        [JsonProperty("Net_Cost")]
        public double NetCost { get; set; }

        [JsonProperty("Emboss_Line_2")]
        public string TruckId { get; set; }

        public override string ToString()
        {
            return $"{TruckId}, {TransactionDate}, {NetCost}";
        }
    }
}