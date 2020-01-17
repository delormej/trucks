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
        private int _year;
        private int[] _weeks;
        private List<FuelCharge> _charges;
        private Task _loading;

        public IEnumerable<FuelCharge> Charges { get { return _charges; }}

        public FuelChargeRepository() 
        {
        }

        public FuelChargeRepository(int year, int[] weeks)
        {
            _year = year;
            _weeks = weeks;

            // Lazy load the fuel charges.
            _loading = GetFuelChargesAsync();
        }

        public double GetFuelCharges(int year, int week, int truckId)
        {
            _loading.Wait();

            string truck = truckId.ToString();
            double fuel = _charges.Where(c => c.TruckId == truck && c.WeekNumber == week).Sum(c => c.NetCost);
            
            return fuel;
        }

        public async Task LoadAsync(string filename)
        {
            _loading = ReadFromFileAsync(filename);   
            await _loading;
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

        private Task ReadFromFileAsync(string filename)
        {
            return Task.Run( () => {
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
                });
        }

        private async Task GetFuelChargesAsync()
        {
            string sqlQueryText = $"SELECT * FROM FuelCharge c";
            sqlQueryText += $" WHERE c.Year = {_year} AND c.WeekNumber IN ({string.Join(',', _weeks)})";
            
            _charges = new List<FuelCharge>();

            using (CosmosClient cosmosClient = GetCosmosClient())
            {
                Container container = cosmosClient.GetContainer(databaseId, "FuelCharge");
                QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
                
                await foreach (FuelCharge item in 
                    container.GetItemQueryIterator<FuelCharge>(queryDefinition))
                        _charges.Add(item);
            }
        }
    }
}
