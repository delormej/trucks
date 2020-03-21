using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

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

    public class CreateSettlementOptions
    {
        public int Year;
        public string[] Companies;
        public int[] Weeks;
        public int TruckId;

        public static CreateSettlementOptions LastWeek()
        {
            int week, year;
            Tools.GetWeekNumber(DateTime.Now, out week, out year);

            return new CreateSettlementOptions() {
                Year = year,
                Weeks = new int[] { week }
            };
        }

        public override string ToString()
        {
            // TODO: stringify this object in some way.
            return "options...";
        }
    }

    /// <summary>
    /// Primary interface for working with settlement statements.
    /// </summary>
    public class SettlementService
    {
        public delegate void NewSettlementEventHandler(object sender, NewSettlementEventArgs e);
        
        /// <summary>
        /// Raised when a settlement has been downloaded from panther.
        /// </summary>
        public event NewSettlementEventHandler OnNewSettlement;

        /// <summary>
        /// Creates Excel settlement files for the specified options from existing settlement history stored
        /// in the repo and returns the list of the file names created.
        /// </summary>
        public async Task<List<string>> CreateSettlementsAsync(CreateSettlementOptions options)
        {
            System.Console.WriteLine($"Creating settlements for: {options}");

            List<string> settlementFiles = new List<string>();

            FuelChargeRepository fuelRepository = new FuelChargeRepository(options.Year, options.Weeks);
            SettlementRepository settlementRepository = new SettlementRepository();    
            List<SettlementHistory> settlements = await 
                settlementRepository.GetSettlementsByWeekAsync(options.Year, options.Weeks);

            if (settlements.Count() > 0)
            {
                if (options.TruckId > 0)
                    settlements = FilterSettlementsByTruck(settlements, options.TruckId).ToList();

                SettlementWorkbookGenerator generator = new SettlementWorkbookGenerator(settlements, fuelRepository);
                foreach (string driver in GetDrivers(settlements, options.TruckId))
                {
                    string file = generator.Generate(options.Year, options.Weeks, driver);
                    settlementFiles.Add(file);
                }
            }
            else
            {
                System.Console.WriteLine($"No settlements found for the specified {options}");
            }
        
            return settlementFiles;
        }

        /// <summary>
        /// Updates all settlement headers in the backing repo with values from panther web site and
        /// return a list of settlement ids updated.
        /// </summary>
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
        /// Helper method that forces an update on all settlements.  This is normally used to update
        /// the serialized object after a schema change in the SettlementHistory object.
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
            var repoTask = repository.GetSettlementsAsync();
            var pantherTask = panther.GetSettlementsAsync();

            await Task.WhenAll(repoTask, pantherTask);

            List<SettlementHistory> savedSettlements = repoTask.Result;
            List<SettlementHistory> settlements = pantherTask.Result;

            // Don't try to convert settlements we've already persisted.
            List<SettlementHistory> settlementsToDownload = settlements.Except(savedSettlements, new SettlementHistoryComparer())
                .OrderByDescending(s => s.SettlementDate)
                .Take(max)
                .ToList();

            return settlementsToDownload;            
        }        

        private IEnumerable<SettlementHistory> FilterSettlementsByTruck(
                IEnumerable<SettlementHistory> settlements, int truckid)
        {
            return settlements.Where(
                        s => s.Credits.Where(c => c.TruckId == truckid).Count() > 0
                    );
        }

        /// <summary>
        /// Gets the list of drivers from the settlements, optionally for a given truck.
        /// </summary>
        private IEnumerable<string> GetDrivers(List<SettlementHistory> settlements, int? truckid)
        {
            IEnumerable<string> drivers = settlements.SelectMany(s => 
                    s.Credits.Where(c => truckid != null ? c.TruckId == truckid : true)
                    .Select(c => c.Driver)).Distinct();                
            
            return drivers;
        }
    }
}
