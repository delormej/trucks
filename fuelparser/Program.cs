using System;
using System.IO;
using System.Text;
using ChoETL;
using System.Text.Json.Serialization;
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
                    var l = w.CastTo<List<FuelCharge>>();
                }
            }

            Console.WriteLine(sb.ToString());
        }
    }

    class FuelCharge
    {
        [JsonPropertyName("Transaction_Date")]
        public string TransactionDate { get; set; }
        
        [JsonPropertyName("Transaction_Time")]
        public string TransactionTime { get; set; }

        [JsonPropertyName("Net_Cost")]
        public double NetCost { get; set; }

        [JsonPropertyName("Emboss_Line_2")]
        public int TruckId { get; set; }
    }
}
