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

        public string Generate(int year, int[] weeks, int truck)
        {
            string outputFile = null;
            SettlementWorkbook workbook = null;
            
            try
            {
                foreach (int week in weeks)
                {
                    DateTime settlementDate = GetSettlementDate(week, truck);
                    SettlementHistory settlement = GetSettlement(week, truck);
                    IEnumerable<Deduction> deductions = settlement.Deductions.Where(d => d.TruckId == truck);
                    IEnumerable<Credit> credits = settlement.Credits.Where(c => c.TruckId == truck);

                    if (workbook == null)
                    {
                        string driver = GetDriver(credits);
                        if (driver != null)
                        {
                            workbook = new SettlementWorkbook(year, truck, driver, settlementDate);
                            outputFile = workbook.Create();
                        }
                        // TODO: if no driver, these were entries attributable back to corp... how do we handle?
                    }
                    if (workbook == null)
                        continue;

                    workbook.AddSheet(week);
                    workbook.AddSettlementId(settlement.SettlementId);

                    if (_fuelRepository != null)
                    {
                        double fuel = _fuelRepository.GetFuelCharges(week, truck);
                        workbook.AddFuelCharge(fuel);
                        credits = GetCreditsWithoutComchek(settlement, truck);
                    }

                    workbook.AddCredits(credits);                    
                    
                    double occInsurance = GetOccupationalInsurance(deductions);
                    if (occInsurance > 0)
                        workbook.AddOccupationalInsurance(occInsurance);
                        
                    workbook.Save();
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

        private string GetDriver(IEnumerable<Credit> credits)
        {
            // TODO: Driver can be different for each line... how do we reconcile this??
            string driver = credits.Where(c => c.CreditDescriptions == "FUEL SURCHARGE CREDIT")
                .Select(c => c.Driver).FirstOrDefault();
            return driver;
        }

        private SettlementHistory GetSettlement(int week, int truck)
        {
            SettlementHistory settlement = null;
            foreach (var s in _settlements.Where(s => s.WeekNumber == week))
            {
                if (s.Credits.Where(c => c.TruckId == truck).Count() > 0)
                {
                    settlement = s;
                    break;
                }
            }

            return settlement;
        }

        private DateTime GetSettlementDate(int week, int truck)
        {
            return _settlements.Where(
                        s => s.WeekNumber == week 
                        && s.Credits.Where(c => c.TruckId == truck).Count() > 0
                    ).First().SettlementDate;            
        }

        private IEnumerable<Credit> GetCreditsWithoutComchek(SettlementHistory settlement, int truck)
        {
            IEnumerable<Credit> credits = settlement.Credits.Where(c => c.TruckId == truck);
            var toRemove = credits.Where(c => c.AdvanceDescription == "COMCHEK PRO ADVANCE");
            return credits.Except(toRemove); 
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