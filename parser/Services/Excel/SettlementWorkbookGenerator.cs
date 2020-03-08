using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Trucks
{
    public class SettlementWorkbookGenerator
    {
        private List<SettlementHistory> _settlements;
        private FuelChargeRepository _fuelRepository;

        public SettlementWorkbookGenerator(List<SettlementHistory> settlements, 
                FuelChargeRepository fuelRepository = null)
        {
            this._settlements = settlements;
            this._fuelRepository = fuelRepository;
        }

        public string Generate(int year, int[] weeks, string driver)
        {
            string outputFile = null;
            SettlementWorkbook workbook = null;
            
            try
            {
                foreach (int week in weeks)
                {
                    SettlementHistory settlement = GetSettlement(week, driver);
                    if (settlement == null)
                    {
                        System.Console.WriteLine($"No settlement found for {driver} on week {week}.");
                        continue;
                    }
                    int truck = GetTruckForDriver(settlement, driver);
                    IEnumerable<Deduction> deductions = settlement.Deductions.Where(d => d.TruckId == truck);
                    IEnumerable<Credit> credits = settlement.Credits.Where(c => c.TruckId == truck);

                    if (workbook == null)
                    {
                        workbook = new SettlementWorkbook(year, truck, driver);
                        outputFile = workbook.Create();
                    }
                    if (workbook == null)
                    {
                        System.Console.WriteLine($"No workbook created for truck {truck} on week {week}.");
                        continue;
                    }

                    if (workbook.Truck != truck)
                        workbook.Truck = truck; 
                    
                    workbook.AddSheet(week, GetSheetSettlementDate(settlement));
                    workbook.AddSettlementId(settlement.SettlementId);

                    //
                    // TODO: currently this is derived *IF* we find fuel charges, however
                    // this logic is actually based on whether the DRIVER is setup for 
                    // comchek or not.  Need to fix this.
                    //
                    #warning FIX THIS: Need to get Comchek flag from Driver.
                    bool ignoreComchek = false;
                    double fuel = _fuelRepository.GetFuelCharges(year, week, truck);
                    if (fuel > 0)
                    {
                        workbook.AddFuelCharge(fuel);
                        ignoreComchek = true;
                    }

                    workbook.AddCredits(credits, ignoreComchek);                    
                    
                    double occInsurance = GetOccupationalInsurance(deductions);
                    if (occInsurance > 0)
                        workbook.AddOccupationalInsurance(occInsurance);
                        
                    workbook.Save();

                    System.Console.WriteLine($"Created {outputFile} with {credits.Count()} credit(s), ${fuel.ToString("0.00")} in fuel from {settlement.id}:{settlement.SettlementDate.ToString("yyyy-MM-dd")} for company {settlement.CompanyId}.");
                }
            }
            catch (Exception e)
            {
                System.Console.WriteLine($"Error generating workbook {outputFile ?? "null"}\n\t{e.Message}");
            }
            finally
            {
                if (workbook != null)
                    workbook.Dispose();
            }

            return outputFile;
        }

        private DateTime GetSheetSettlementDate(SettlementHistory settlement)
        {
            DateTime sheetSettlementDate = settlement.SettlementDate.AddDays(7);
            if (sheetSettlementDate.DayOfWeek != DayOfWeek.Friday)
                throw new ApplicationException($"Settlement date must be a Friday: {settlement.SettlementDate}");            
            return sheetSettlementDate;
        }

        private string GetDriver(IEnumerable<Credit> credits)
        {
            // TODO: Driver can be different for each line... how do we reconcile this??
            string driver = credits.Where(c => c.CreditDescriptions == "FUEL SURCHARGE CREDIT")
                .Select(c => c.Driver).FirstOrDefault();
            return driver;
        }

        private SettlementHistory GetSettlement(int week, string driver)
        {
            return _settlements.Where(
                        s => s.WeekNumber == week 
                        && s.Credits.Where(c => c.Driver == driver).Count() > 0
                    ).FirstOrDefault();            
        }

        private int GetTruckForDriver(SettlementHistory settlement, string driver)
        {
            var credit = settlement.Credits.Where(c => c.Driver == driver)
                .FirstOrDefault();
            if (credit != null)
                return credit.TruckId;
            else
                throw new ApplicationException($"Unable to find a truckid for driver {driver} in settlement {settlement.id}");
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

        // Externalize business logic into a predicate?
        // public Func<SettlementHistory, bool> DeductionPredicate { get; set; }

            // if company is 44510 and truck is NOT Andrew Rowan, then don't include 
            // COMCHEK PRO ADVANCE

            // if (s.CompanyId == 44510)
            // {
            //     s.Deductions.Where(d => d.TruckId != 13357);
            // }

            // ADVANCE FEE PLAN F gets added if ??
    }
}