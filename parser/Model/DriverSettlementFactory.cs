using System;
using System.Collections.Generic;
using System.Linq;
using jasondel.Tools;

namespace Trucks
{
    /// <summary>
    /// Helper object for creating a DriverSettlement for drivers in a settlement.
    /// </summary>
    public class DriverSettlementFactory
    {
        /// <summary>
        /// Creates DriverSettlements model objects for each driver for each week.
        /// </summary>
        public static List<DriverSettlement> CreateDriverSettlements(IEnumerable<SettlementHistory> settlements, int? truckId = null)
        {
            Logger.Log($"Creating DriverSettlements for {settlements.Count()} settlement(s).");
            
            if (truckId != null)
                settlements = settlements.FilterByTruck((int)truckId);

            List<DriverSettlement> driverSettlements = new List<DriverSettlement>();
            foreach (var settlement in settlements)
            {
                DriverSettlementFactory generator = new DriverSettlementFactory(settlement);
                foreach (var driver in settlement.GetDrivers(null))
                    driverSettlements.Add(generator.Create(driver));
            }

            return driverSettlements;
        }

        private SettlementHistory _settlement;
        private FuelChargeRepository _fuelRepository;
        private int _week, _year;

        public DriverSettlementFactory(SettlementHistory settlement)
        {
            _fuelRepository = new FuelChargeRepository();
            _settlement = settlement;
            Tools.GetWeekNumber(_settlement.SettlementDate, out _week, out _year);
        }

        /// <summary>
        /// Creates a DriverSettlement for a specific driver.
        /// </summary>
        public DriverSettlement Create(string driver)
        {
            Logger.Log($"Creating DriverSettlement for {_year}/{_week}, {driver}");
            
            DriverSettlement driverSettlement = new DriverSettlement()
            {
                Driver = driver,
                CompanyId = _settlement.CompanyId,
                WeekId = _week,
                Year = _year,
                DriverSettlementDate = GetSettlementDate(),
                TruckId = GetTruckId(driver)
            };
            driverSettlement.FuelCharges = GetFuelCharges(driverSettlement.TruckId);
            driverSettlement.Credits = GetCredits(driverSettlement.TruckId);
            driverSettlement.Deductions = GetDeductions(driverSettlement.TruckId);
            driverSettlement.OccupationalInsurance = GetOccupationalInsurance(driverSettlement.Deductions);

            return driverSettlement;
        }

        private int GetTruckId(string driver)
        {
            int[] truckIds = _settlement.GetTruckIds(driver);
            
            if (truckIds?.Length == 0)
                throw new ApplicationException($"Unable to find truck Id for {driver} in {_settlement.SettlementId}");
            
            if (truckIds.Length > 1)
                Logger.Log($"WARNING! More than 1 truck found for {driver} in {_settlement.SettlementId}, returning just the first {truckIds[0]}.");
            
            return truckIds[0];
        }

        private double GetFuelCharges(int truck) 
        {
            /* TODO: -- this needs to come from  driver ss# */
            double fuel = _fuelRepository.GetFuelCharges(_year, _week, truck);
            return fuel;
        }

        private IEnumerable<Credit> GetCredits(int truck)
        {
            return _settlement.Credits.Where(c => c.TruckId == truck);
        }

        private IEnumerable<Deduction> GetDeductions(int truck)
        {
            return  _settlement.Deductions.Where(d => d.TruckId == truck);
        }

        private double GetOccupationalInsurance(IEnumerable<Deduction> deductions)
        {
            double value = 0.0;
            var occupationalInsurance = deductions.Where(d => 
                d.Description == "OCCUPATIONAL INSURANCE").FirstOrDefault();
            if (occupationalInsurance != null)
                value = occupationalInsurance.Amount;

            return value;
        }

        private DateTime GetSettlementDate()
        {
            DateTime sheetSettlementDate = _settlement.SettlementDate.AddDays(7);
            if (sheetSettlementDate.DayOfWeek != DayOfWeek.Friday)
                throw new ApplicationException($"Settlement date must be a Friday: {_settlement.SettlementDate}");            
            return sheetSettlementDate;
        }
   }

    public static class DriverSettlementExtensions
    {
        public static IEnumerable<DriverSettlement> GetByDriver(
            this IEnumerable<DriverSettlement> settlements, string driver)
        {
            return settlements.Where(s => s.Driver == driver);
        }
    }    
}