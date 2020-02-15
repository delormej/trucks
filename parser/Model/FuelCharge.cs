using System;
using System.Text;
using Newtonsoft.Json;
using System.Security.Cryptography;

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
                return GetHash();
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
                if (week == 52)
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

        [JsonProperty("Product")]
        public string Product { get; set; }

        [JsonProperty("Units")]
        public double Units { get; set; }

        public override string ToString()
        {
            return $"{TruckId}, {TransactionDate}, {NetCost}";
        }

        private string GetHash()
        {
            string raw = $"{TruckId?.Trim()}{TransactionDate?.Trim()}{TransactionTime?.Trim()}{Units.ToString()}{NetCost.ToString()}";
            return GetMd5Hash(raw);        
        }

        static string GetMd5Hash(string input)
        {
            MD5 md5Hash = MD5.Create();

            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte (up to 8) of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }        
    }
}