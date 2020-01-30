using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Trucks
{
    public class SettlementService
    {

        // UpdateSettlementHeaders
        // UpdateAllSettlements
        // ConsolidateSettlements
        // CreateSettlementStatement
        // PrintSettlementHeader

        public string[] CreateSettlementsForYear(int year)
        {
            int[] weeks = Enumerable.Range(1, 52).ToArray();
            return CreateSettlementStatement(year, weeks);     
        }

        public string[] CreateSettlementStatement(int year, int[] weeks, int? truckid = null)
        {
            System.Console.WriteLine($"Creating settlements {year}/{weeks[0]} {truckid?.ToString()}");
            List<string> files = new List<string>();
           
            Task.Run( async () => 
            {   
                FuelChargeRepository fuelRepository = new FuelChargeRepository(year, weeks);
                SettlementRepository settlementRepository = new SettlementRepository();    
                List<SettlementHistory> settlements = await settlementRepository.GetSettlementsByWeekAsync(year, weeks);
                if (settlements.Count() > 0)
                {
                    if (truckid != null)
                        settlements = FilterSettlementsByTruck(settlements, (int)truckid).ToList();

                    SettlementWorkbookGenerator generator = new SettlementWorkbookGenerator(settlements, fuelRepository);
                    foreach (string driver in GetDrivers(settlements, truckid))
                    {
                        string file = generator.Generate(year, weeks, driver);
                        files.Add(file);
                    }
                }
                else
                {
                    System.Console.WriteLine($"No settlements found for the specified {year} and {string.Join(",", weeks)}");
                }
            }
            ).Wait();
            
            return files.ToArray();
        }

        public string[] UpdateHeadersFromPanther(string company, string pantherPassword)
        {
            System.Console.WriteLine($"Updating settlements for company: {company}.");
            List<SettlementHistory> settlementsToUpdate = null;

            var task = Task.Run(async () => 
            {
                PantherClient panther = new PantherClient(company, pantherPassword);
                List<SettlementHistory> settlements = await panther.GetSettlementsAsync();

                SettlementRepository repository = new SettlementRepository();
                List<SettlementHistory> savedSettlements = await repository.GetSettlementsAsync();

                settlementsToUpdate = 
                    settlements.Intersect(savedSettlements, new SettlementHistoryComparer())
                    .ToList();
                repository.SaveSettlements(settlementsToUpdate);
            });
            task.Wait();

            string[] settlementIds = settlementsToUpdate?.Select(s => s.SettlementId).ToArray(); 
            return settlementIds;
        }

        /// <summary>
        /// Forces an update on all settlements.  This is normally used to update the serialized 
        /// object after a schema change.
        /// </summary>
        public string[] UpdateAll()
        {
            System.Console.WriteLine($"Updating all settlements.");
            List<SettlementHistory> savedSettlements = null;

            var task = Task.Run(async () => 
            {
                SettlementRepository repository = new SettlementRepository();
                savedSettlements = await repository.GetSettlementsAsync();
                repository.SaveSettlements(savedSettlements);
            });
            task.Wait();   

            string[] settlementIds = savedSettlements?.Select(s => s.SettlementId).ToArray(); 
            return settlementIds;             
        }

        /// <summary>
        /// Downloads and returns 'max' settlements from panther that we have not persisted, ordered by
        /// descending date.
        /// <summary>
        public async Task<List<SettlementHistory>> DownloadMissingSettlements(string company, string pantherPassword, int max = 10)
        {
            PantherClient panther = new PantherClient(company, pantherPassword);
            List<SettlementHistory> settlements = await panther.GetSettlementsAsync();

            SettlementRepository repository = new SettlementRepository();
            List<SettlementHistory> savedSettlements = await repository.GetSettlementsAsync();

            // Don't try to convert settlements we've already persisted.
            List<SettlementHistory> settlementsToDownload = settlements.Except(savedSettlements, new SettlementHistoryComparer())
                .OrderByDescending(s => s.SettlementDate)
                .Take(max)
                .ToList();

            List<SettlementHistory> settlementsToConvert = 
                await panther.DownloadSettlementsAsync(settlementsToDownload);

            return settlementsToConvert;
        }

        private IEnumerable<SettlementHistory> FilterSettlementsByTruck(
                IEnumerable<SettlementHistory> settlements, int truckid)
        {
            return settlements.Where(
                        s => s.Credits.Where(c => c.TruckId == truckid).Count() > 0
                    );
        }

        private IEnumerable<string> GetDrivers(List<SettlementHistory> settlements, int? truckid)
        {
            IEnumerable<string> drivers = settlements.SelectMany(s => 
                    s.Credits.Where(c => truckid != null ? c.TruckId == truckid : true)
                    .Select(c => c.Driver)).Distinct();                
            
            return drivers;
        }
    }
}