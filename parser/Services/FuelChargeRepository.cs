using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using ChoETL;
using Newtonsoft.Json;

namespace Trucks
{
    public class FuelChargeRepository
    {
        List<FuelCharge> _charges;

        public FuelChargeRepository(string filename)
        {
            Load(filename);
        }

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
            
            return _charges.Where(c => GetWeek(c.TransactionDate) == week);

            int GetWeek(string date)
            { 
                return Tools.GetWeekNumber(DateTime.Parse(date));
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
