using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using ChoETL;
using Newtonsoft.Json;
using Azure.Cosmos;
using System.Threading.Tasks;
using jasondel.Tools;

namespace Trucks
{
    public class FuelChargeRepository : Repository
    {
        private int _year;
        private int _week;
        private List<FuelCharge> _charges;
        private Task _loading;

        public IEnumerable<FuelCharge> Charges { get { return _charges; }}

        private FuelChargeRepository() 
        {
        }

        public FuelChargeRepository(int year, int week)
        {
            _year = year;
            _week = week;

            // Lazy load the fuel charges.
            _loading = GetFuelChargesAsync();
        }

        public double GetFuelCharges(int truckId)
        {
            _loading.Wait();
            string truck = truckId.ToString();
            double fuel = _charges.Where(c => c.TruckId == truck && c.WeekNumber == _week).Sum(c => c.NetCost);
            
            return fuel;
        }

        /// <summary>
        /// Persists all charges to backing datastore.
        /// </summary>
        public async Task SaveAsync(IEnumerable<FuelCharge> charges)
        {
            using (CosmosClient cosmos = GetCosmosClient())
            {
                foreach (FuelCharge charge in charges)
                {
                    try
                    {
                        await AddItemsToContainerAsync<FuelCharge>(cosmos, charge);
                        Logger.Log($"Saved {charge.id}");
                    }
                    catch (Exception e)
                    {
                        Logger.Log($"Error saving {charge.id}\n\t{e.Message}");
                    }
                }
            }
        }

        public static async Task SaveAsync(string file)
        {
            Logger.Log($"Saving {file} fuel charges to database.");
            List<FuelCharge> charges = await ReadFromFileAsync(file);

            FuelChargeRepository repo = new FuelChargeRepository();
            await repo.SaveAsync(charges);
            Logger.Log($"Saved {charges?.Count()} charge(s).");
        }

        private static async Task<List<FuelCharge>> ReadFromFileAsync(string filename)
        {

            string csv = await File.ReadAllTextAsync(filename);
            StringBuilder sb = new StringBuilder();
            using (var p = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                )
            {
                using (var w = new ChoJSONWriter(sb))
                    w.Write(p);
            }

            List<FuelCharge> charges = JsonConvert.DeserializeObject<List<FuelCharge>>(sb.ToString());

            return charges;
        }

        private async Task GetFuelChargesAsync()
        {
            Logger.Log($"Loading fuel charges for week {_week}.");
            string sqlQueryText = $"SELECT * FROM FuelCharge c";
            sqlQueryText += $" WHERE c.Year = {_year} AND c.WeekNumber = {_week}";
            
            _charges = new List<FuelCharge>();

            using (CosmosClient cosmosClient = GetCosmosClient())
            {
                Container container = cosmosClient.GetContainer(databaseId, "FuelCharge");
                QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
                
                await foreach (FuelCharge item in 
                    container.GetItemQueryIterator<FuelCharge>(queryDefinition))
                        _charges.Add(item);
            }

            Logger.Log($"Loaded {_charges.Count} fuel charges for week {_week}.");
        }

        protected override async Task CreateContainerAsync()
        {
            await CreateContainerAsync("FuelCharge", "/TruckId");
        }        
    }
}
