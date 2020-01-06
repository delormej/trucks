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
        List<FuelCharge> _charges;

        public FuelChargeRepository()
        {
        }

        public List<FuelCharge> FuelCharges { get { return _charges; }}

        public double GetFuelCharges(int year, int week, int truckId)
        {
            double fuel = 0.0;
            string truck = truckId.ToString();
            IEnumerable<FuelCharge> charges = null;
            var task = Task.Run( async () => {
                charges = await GetFuelChargesAsync(year, week);
                if (charges != null)
                    charges = charges.Where(f => f.TruckId == truck);
            });
            task.Wait();

            if (charges?.Count() > 0)
                fuel = charges.Sum(c => c.NetCost);
            
            if (fuel > 0)
                System.Console.WriteLine($"Found ${fuel.ToString("0.00")} in fuel.");
            else
                System.Console.WriteLine($"No fuel found for {truck}.");

            return fuel;
        }

        private async Task<IEnumerable<FuelCharge>> GetFuelChargesAsync(int year, int week)
        {
            if (_charges == null)
            {
                _charges = new List<FuelCharge>();
                using (CosmosClient cosmosClient = GetCosmosClient())
                {
                    try 
                    {
                        string sqlQueryText = $"SELECT * FROM FuelCharge c";
                        sqlQueryText += $" WHERE c.Year = {year} AND c.WeekNumber IN ({week})";

                        Container container = cosmosClient.GetContainer(databaseId, "FuelCharge");
                        QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
                        
                        await foreach (FuelCharge item in 
                            container.GetItemQueryIterator<FuelCharge>(queryDefinition))
                                _charges.Add(item);
                    }
                    catch (Exception e)
                    {
                        System.Console.WriteLine($"Error occurred while retrieving FuelCharge for week: {week}:\n\t" +
                            e.Message);
                        return null;
                    }
                }   
            }
            return _charges;
        }

        /// <summary>
        /// Persists all charges to backing datastore.
        /// </summary>
        public void SaveAsync()
        {
            using (CosmosClient cosmos = GetCosmosClient())
            {
                foreach (FuelCharge charge in _charges)
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

        public void Load(string filename)
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

            _charges = JsonConvert.DeserializeObject<List<FuelCharge>>(sb.ToString());
        }
    }
}
