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

        public FuelChargeRepository(string filename)
        {
            Load(filename);
        }

        public List<FuelCharge> FuelCharges { get { return _charges; }}

        public double GetFuelCharges(int week, int truckId)
        {
            double fuel = 0.0;
            string truck = truckId.ToString();
            IEnumerable<FuelCharge> charges = GetFuelCharges(week).Where(f => f.TruckId == truck);
            if (charges?.Count() > 0)
                fuel = charges.Sum(c => c.NetCost);
            
            if (fuel > 0)
                System.Console.WriteLine($"Found ${fuel.ToString("0.00")} in fuel.");
            else
                System.Console.WriteLine($"No fuel found for {truck}.");

            return fuel;
        }

        public IEnumerable<FuelCharge> GetFuelCharges(int week)
        {
            if (_charges == null)
                throw new ApplicationException("No fuel charges available.");
            
            return _charges.Where(c => c.Week == week);
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

        private void Load(string filename)
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
