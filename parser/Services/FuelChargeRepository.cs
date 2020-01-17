using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using ChoETL;
using Newtonsoft.Json;
using Azure.Cosmos;
using System.Threading.Tasks;

namespace Trucks
{
    public class FuelChargeRepository : Repository
    {
        public FuelChargeRepository()
        {
        }

        public double GetFuelCharges(int year, int week, int truckId)
        {
            string truck = truckId.ToString();
         
            List<FuelCharge> charges = null;
            Task.Run( async () => {
                charges = await GetFuelChargesAsync(year, week, truckId);
            }).Wait();

            double fuel = 0.0;
            if (charges?.Count() > 0)
                fuel = charges.Sum(c => c.NetCost);
            
            return fuel;
        }

        private async Task<List<FuelCharge>> GetFuelChargesAsync(int year, int week, int truckId)
        {
            string sqlQueryText = $"SELECT * FROM FuelCharge c";
            sqlQueryText += $" WHERE c.Emboss_Line_2 = '{truckId.ToString()}' AND c.Year = {year} AND c.WeekNumber = {week}";
            
            List<FuelCharge> charges = new List<FuelCharge>();

            using (CosmosClient cosmosClient = GetCosmosClient())
            {
                Container container = cosmosClient.GetContainer(databaseId, "FuelCharge");
                QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
                
                await foreach (FuelCharge item in 
                    container.GetItemQueryIterator<FuelCharge>(queryDefinition))
                        charges.Add(item);
            }
            
            return charges;
        }

        /// <summary>
        /// Persists all charges to backing datastore.
        /// </summary>
        public void SaveAsync(IEnumerable<FuelCharge> charges)
        {
            using (CosmosClient cosmos = GetCosmosClient())
            {
                foreach (FuelCharge charge in charges)
                {
                    try
                    {
                        AddItemsToContainerAsync<FuelCharge>(cosmos, charge).Wait();
                        System.Console.WriteLine($"Saved {charge.id}");
                    }
                    catch (Exception e)
                    {
                        System.Console.WriteLine($"Error saving {charge.id}\n\t{e.Message}");
                    }
                }
            }
        }

        public IEnumerable<FuelCharge> Load(string filename)
        {
            string csv = File.ReadAllText(filename);
            StringBuilder sb = new StringBuilder();
            using (var p = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                )
            {
                using (var w = new ChoJSONWriter(sb))
                    w.Write(p);
            }

            return JsonConvert.DeserializeObject<List<FuelCharge>>(sb.ToString());
        }
    }
}
