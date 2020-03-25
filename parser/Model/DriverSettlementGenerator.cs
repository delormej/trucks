using System;
using System.Collections.Generic;
using System.Linq;
using jasondel.Tools;

namespace Trucks
{
    public class DriverSettlementGenerator
    {
        private SettlementHistory _settlement;
        private int _week, _year;

        public DriverSettlementGenerator(SettlementHistory settlement)
        {
            _settlement = settlement;
            Tools.GetWeekNumber(_settlement.SettlementDate, out _week, out _year);

        }

        public DriverSettlement Create(string driver)
        {
            Logger.Log($"Creating DriverSettlement for {_year}/{_week}, {driver}");
            
            DriverSettlement settlement = new DriverSettlement();
            settlement.Driver = driver;
            settlement.WeekId = _week;
            settlement.Year = _year;
            settlement.FuelCharges = GetFuelCharges(driver);
            settlement.Credits = GetCredits();
            settlement.Deductions = GetDeductions();
            settlement.OccupationalInsurance = GetOccupationalInsurance();
            settlement.DriverSettlementDate = GetSettlementDate();

            return settlement;
        }

        private double GetFuelCharges(string driver) 
        {
            // int week 
            return 0;
        }

        private List<Credit> GetCredits()
        {
            return null;
        }

        private List<Deduction> GetDeductions()
        {
            return null;
        }

        private double GetOccupationalInsurance()
        {
            return 0;
        }
        private DateTime GetSettlementDate()
        {
            return DateTime.Now;
        }
    }
}