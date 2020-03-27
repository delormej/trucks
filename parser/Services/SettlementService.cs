using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using jasondel.Tools;

namespace Trucks
{
    public class NewSettlementEventArgs : EventArgs
    {
        public NewSettlementEventArgs(ConversionJob job)
        {
            this.Job = job;
        }

        public ConversionJob Job { get; set; }
    }

    /// <summary>
    /// Primary interface for working with settlement statements.
    /// </summary>
    public class SettlementService
    {
        private List<SettlementHistory> _settlementHeaders;

        public delegate void NewSettlementEventHandler(object sender, NewSettlementEventArgs e);
        
        /// <summary>
        /// Raised when a settlement has been downloaded from panther.
        /// </summary>
        public event NewSettlementEventHandler OnNewSettlement;

        public SettlementService()
        {
            _settlementHeaders = new List<SettlementHistory>();
        }

        private List<SettlementHistory> SettlementHeaders { get { return _settlementHeaders; } }

        /// <summary>
        /// Creates Excel settlement files for the specified options from existing settlement history stored
        /// in the repo and returns the list of the file names created.
        /// </summary>
        public async Task<List<string>> CreateSettlementsAsync(CreateSettlementOptions options)
        {
            Logger.Log($"Creating settlements for: {options}");

            List<string> settlementFiles = new List<string>();
            List<SettlementHistory> settlements = await GetSettlementsAsync(options);

            if (settlements.Count() > 0)
            {
                List<DriverSettlement> driverSettlements = DriverSettlementFactory
                    .CreateDriverSettlements(settlements, options.TruckId);

                foreach (string driver in settlements.GetDrivers(options.TruckId))
                {
                    Logger.Log($"Creating settlement workbook for {driver}");
                    string file = SettlementWorkbookFactory.Create(driverSettlements.GetByDriver(driver));
                    settlementFiles.Add(file);
                }
            }
            else
            {
                Logger.Log($"No settlements found for the specified {options}");
            }
        
            return settlementFiles;
        }

        public async Task<List<SettlementHistory>> GetSettlementsAsync(CreateSettlementOptions options)
        {
            SettlementRepository settlementRepository = new SettlementRepository();    
            List<SettlementHistory> settlements = await 
                settlementRepository.GetSettlementsByWeekAsync(options.Year, options.Weeks);

            if (options.TruckId > 0)
                settlements = settlements.FilterByTruck(options.TruckId).ToList();

            return settlements;            
        }

        /// <summary>
        /// Updates all settlement headers in the backing repo with values from panther web site and
        /// return a list of settlement ids updated.
        /// </summary>
        public string[] UpdateHeadersFromPanther(PantherClient panther)
        {
            Logger.Log($"Updating settlements for company: {panther.Company}.");
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
        /// Helper method that forces an update on all settlements.  This is normally used to update
        /// the serialized object after a schema change in the SettlementHistory object.
        /// </summary>
        public string[] UpdateAll()
        {
            Logger.Log($"Updating all settlements.");
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
        public async Task DownloadMissingSettlements(PantherClient panther, int max = 10)
        {
            List<SettlementHistory> settlementsToDownload = 
                await GetMissingSettlementsAsync(panther, max);

            await foreach(var download in panther.DownloadSettlementsAsync(settlementsToDownload))
            {
                ConversionJob job = new ConversionJob();
                job.SourceXls = download.Key;
                job.SourceTimestamp = DateTime.Now;
                job.Company = download.Value.CompanyId.ToString();
                job.SettlementId = download.Value.SettlementId;
                job.SettlementDate = download.Value.SettlementDate;                
                
                if (OnNewSettlement != null)
                    OnNewSettlement(this, new NewSettlementEventArgs(job));
            }
        }

        /// <summary>
        /// Returns the intersection of settlements available on Panther which are not in the repository.
        /// </summary>
        public async Task<List<SettlementHistory>> GetMissingSettlementsAsync(PantherClient panther, int max = 10)
        {
            SettlementRepository repository = new SettlementRepository();
            SettlementHistoryComparer comparer = new SettlementHistoryComparer();

            var repoTask = repository.GetSettlementsAsync();
            var pantherTask = LoadSettlementsAsync(panther);

            await Task.WhenAll(repoTask, pantherTask);

            List<SettlementHistory> savedSettlements = repoTask.Result;
            List<SettlementHistory> settlements = pantherTask.Result;
            
            if (settlements == null)
                throw new ApplicationException("No settlements found on Panther!");

            // Don't try to convert settlements we've already persisted.
            List<SettlementHistory> settlementsToDownload = 
                settlements.Except(savedSettlements, comparer)
                    .OrderByDescending(s => s.SettlementDate)
                    .Take(max)
                    .ToList();

            return settlementsToDownload;            
        }       

        /// <summary>
        /// Loads settlements from Panther, updates local cache.
        /// </summary>
        public async Task<List<SettlementHistory>> LoadSettlementsAsync(PantherClient panther)
        {
            if (_settlementHeaders.ContainsCompany(int.Parse(panther.Company)))
            {
                Logger.Log($"Already have settlements cached for {panther.Company}");
                return null;
            }

            SettlementHistoryComparer comparer = new SettlementHistoryComparer();
            var settlements = await panther.GetSettlementsAsync();
            // Add, but don't duplicate.
            _settlementHeaders.AddRange(settlements.Except(_settlementHeaders, comparer));
            
            return settlements;
        }

        /// <summary>
        /// Combines the "body" settlement, often loaded from file containing credits and deductions
        /// with more detailed metadata which is often obtained from the Panther site.
        /// Returns a complete settlement.
        /// </summary>
        public List<SettlementHistory> MergeHeaders(List<SettlementHistory> settlements)
        {
            List<SettlementHistory> mergedSettlements = new List<SettlementHistory>();
            foreach (SettlementHistory body in settlements)
            {
                SettlementHistory settlement = _settlementHeaders?.FindSettlementById(body.SettlementId);
                if (settlement == null)
                    Logger.Log($"Cannot find header for {body.SettlementId}");
                else
                {
                    settlement.Credits = body.Credits;
                    settlement.Deductions = body.Deductions;
                }
                mergedSettlements.Add(settlement);
            }
                
            return mergedSettlements;
        }
    }
}
