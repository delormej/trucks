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

        public string[] UpdateHeadersFromPanther(PantherClient panther)
        {
            System.Console.WriteLine($"Updating settlements for company: {panther.Company}.");
            List<SettlementHistory> settlementsToUpdate = null;

            var task = Task.Run(async () => 
            {
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

        public async Task<IEnumerable<ConversionJob>> StartConversion(PantherClient panther, ExcelConverter converter, int max = 10)
        {
            System.Console.WriteLine("Downloading settlements from panther and uploading to conversion service.");

            List<KeyValuePair<string, SettlementHistory>> downloads = 
                await DownloadMissingSettlements(panther, max);
            List<ConversionJob> jobs = new List<ConversionJob>();
            
            foreach (var download in downloads)
            {
                string filename = download.Key;

                ConversionJob job = new ConversionJob();
                job.Result = await converter.UploadAsync(filename);
                job.Company = download.Value.CompanyId.ToString();
                job.SettlementId = download.Value.SettlementId;
                job.SettlementDate = download.Value.SettlementDate;
             
                QueueUploaded(job);
                jobs.Add(job);
            }       

            return jobs;     
        }

        /// <summary>
        /// Downloads and returns 'max' settlements from panther that we have not persisted, ordered by
        /// descending date.
        /// <summary>
        private async Task<List<KeyValuePair<string, SettlementHistory>>> DownloadMissingSettlements(PantherClient panther, int max = 10)
        {
            List<SettlementHistory> settlements = await panther.GetSettlementsAsync();

            SettlementRepository repository = new SettlementRepository();
            List<SettlementHistory> savedSettlements = await repository.GetSettlementsAsync();

            // Don't try to convert settlements we've already persisted.
            List<SettlementHistory> settlementsToDownload = settlements.Except(savedSettlements, new SettlementHistoryComparer())
                .OrderByDescending(s => s.SettlementDate)
                .Take(max)
                .ToList();

            List<KeyValuePair<string, SettlementHistory>> settlementsToConvert = 
                await panther.DownloadSettlementsAsync(settlementsToDownload);

            return settlementsToConvert;
        }

        /// <summary>
        /// Places the result of the upload on a queue for another process to dequeue when ready.
        /// </summary>
        private void QueueUploaded(ConversionJob job)
        { /* Call DAPR? */ }                

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
