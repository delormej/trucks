using System;
using Newtonsoft.Json;

namespace Trucks
{
    public class FuelCharge
    {
        private string _id;
        private int _week;
        private int _year;
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
            get { return _week; }
            set { _week = value; } 
        }

        public int Year 
        {
            get { return _year; }
            set { _year = value; } 
        }        

        [JsonProperty("Transaction_Date")]
        public string TransactionDate
        {
            get { return _transactionDate; } 
            set
            {
                _transactionDate = value;
                Tools.GetWeekNumber(DateTime.Parse(_transactionDate), out _week, out _year);
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