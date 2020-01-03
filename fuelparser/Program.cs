using System;
using System.IO;
using System.Text;
using ChoETL;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace FuelParser
{
    class Program
    {
        static void Main(string[] args)
        {
            string filename = args[0];
            string csv = File.ReadAllText(filename);
            StringBuilder sb = new StringBuilder();
            using (var p = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                )
            {
                using (var w = new ChoJSONWriter(sb))
                {
                    w.Write(p);
                }
            }

            List<FuelCharge> list = JsonConvert.DeserializeObject<List<FuelCharge>>(sb.ToString());
            foreach (var l in list)
                System.Console.WriteLine(l);

            //Console.WriteLine(sb.ToString());
        }
    }

    class FuelCharge
    {
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
            return $"{TruckId}, {TransactionDate}, {NetCost}";
        }
    }
}
