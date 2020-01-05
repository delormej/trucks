using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Trucks
{
    class Program
    {
        static void Main(string[] args)
        {
            ShowUsage(args);

            string company = Environment.GetEnvironmentVariable("TRUCKCOMPANY");
            string password = Environment.GetEnvironmentVariable("TRUCKPASSWORD");
            string convertApiKey = Environment.GetEnvironmentVariable("ZAMZARKEY");

            if (string.IsNullOrWhiteSpace(company) || 
                string.IsNullOrWhiteSpace(password) || 
                string.IsNullOrWhiteSpace(convertApiKey))
            {
                System.Console.WriteLine("Must set TRUCKCOMPANY, TRUCKPASSWORD, ZAMZARKEY env variables.");
                return;
            }

            if (args.Length < 1)
            {
                DownloadAndConvert(company, password, convertApiKey);
            }
            else
            {
                string command = args[0].ToLower();
                if (command == "uploaded")
                    ProcessUploaded(company, password, convertApiKey);
                else if (command == "downloaded")
                    ProcessDownloaded(company, password, convertApiKey);
                else if (command == "update")
                    UpdateSettlementHeaders(company, password);
                else if (command == "updateall")
                    UpdateAllSettlements();                    
                else if (command == "consolidate")
                    ConsolidateSettlements();
                else if (command == "report")
                    GetReport();
                else if (command == "settlement")
                {
                    int year = int.Parse(args[1]);
                    int week = int.Parse(args[2]);
                    int truck;
                    if (args.Length > 3 && int.TryParse(args[3], out truck))
                    {
                        if (args.Length > 4)
                            CreateSettlementStatement(year, week, truck, args[4]);
                        else
                            CreateSettlementStatement(year, week, truck);
                    }
                    else
                        CreateSettlementStatement(year, week);
                }
            }
        }

        private static void ProcessDownloaded(string company, string pantherPassword, string convertApiKey)
        {
            System.Console.WriteLine("Processing local converted xlsx files.");

            List<SettlementHistory> settlementHeaders = null; 
            List<SettlementHistory> downloadedSettlements = null;
            var getSettlementHeaders = Task.Run( async () => {
                PantherClient panther = new PantherClient(company, pantherPassword);
                settlementHeaders = await panther.GetSettlementsAsync();
            });
            var parseLocalFiles = Task.Run( () =>
                downloadedSettlements = SettlementHistoryParser.ParseLocalFiles(company));
            Task.WaitAll(getSettlementHeaders, parseLocalFiles);

            if (downloadedSettlements.Count > 0)
            {
                System.Console.WriteLine($"Found {downloadedSettlements.Count} to process.");
                List<SettlementHistory> mergedSettlements = 
                    MergeSettlements(settlementHeaders, downloadedSettlements).ToList();
                System.Console.WriteLine($"Merged {mergedSettlements.Count}.");

                if (mergedSettlements.Count > 0)
                {
                    SettlementRepository repository = new SettlementRepository();
                    repository.SaveSettlements(mergedSettlements);            
                }
            }
            else
            {
                System.Console.WriteLine($"No settlements found for company {company}.");
            }
        }

        private static IEnumerable<SettlementHistory> MergeSettlements(
                List<SettlementHistory> settlementHeaders, 
                List<SettlementHistory> downloadedSettlements)
        {
            foreach (var downloadedSettlement in downloadedSettlements)
            {
                var match = settlementHeaders.Where(s => 
                    s.SettlementId == downloadedSettlement.SettlementId)
                    .FirstOrDefault();

                if (match != null)
                {
                    match.Credits = downloadedSettlement.Credits;
                    match.Deductions = downloadedSettlement.Deductions;
                    yield return match;
                }
                else
                {
                    System.Console.WriteLine($"No match while merging settlement {downloadedSettlement.SettlementId}");
                }
            }
        }

        private static void ProcessUploaded(string company, string pantherPassword, string convertApiKey)
        {
            System.Console.WriteLine("Processing files already uploaded to converter.");
            PantherClient panther = new PantherClient(company, pantherPassword);
            ConvertedExcelFiles converted = new ConvertedExcelFiles(convertApiKey);
            converted.Process(panther);
        }

        /// <summary>
        /// Downloads settlements from panther, converts, parses and persists to database.
        /// </summary>
        private static void DownloadAndConvert(string company, string pantherPassword, string convertApiKey, int max = 10)
        {
            System.Console.WriteLine("Downloading settlements from panther and uploading to conversion service.");

            var task = Task.Run(async () => 
            {
                PantherClient panther = new PantherClient(company, pantherPassword);
                List<SettlementHistory> settlementsToConvert = await GetSettlementsToConvert(panther);
                if (settlementsToConvert != null)
                {
                    ConversionOrchestrator orchestrator = new ConversionOrchestrator(settlementsToConvert, convertApiKey);
                    await orchestrator.StartAsync(max);
                }
                else
                {
                    System.Console.WriteLine($"No settlements found to convert for {company}.");
                }
                
            });
            task.Wait();
        }

        private static void UpdateSettlementHeaders(string company, string pantherPassword)
        {
            System.Console.WriteLine($"Updating settlements for company: {company}.");

            var task = Task.Run(async () => 
            {
                PantherClient panther = new PantherClient(company, pantherPassword);
                List<SettlementHistory> settlements = await panther.GetSettlementsAsync();

                SettlementRepository repository = new SettlementRepository();
                List<SettlementHistory> savedSettlements = await repository.GetSettlementsAsync();

                List<SettlementHistory> settlementsToUpdate = 
                    settlements.Intersect(savedSettlements, new SettlementHistoryComparer())
                    .ToList();
                repository.SaveSettlements(settlementsToUpdate);
            });
            task.Wait();           
        }

        private static void UpdateAllSettlements()
        {
            System.Console.WriteLine($"Updating all settlements.");

            var task = Task.Run(async () => 
            {
                SettlementRepository repository = new SettlementRepository();
                List<SettlementHistory> savedSettlements = await repository.GetSettlementsAsync();
                repository.SaveSettlements(savedSettlements);
            });
            task.Wait();                       
        }

        private static async Task<List<SettlementHistory>> GetSettlementsToConvert(PantherClient panther, int max = 10)
        {
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

        private static void CreateSettlementStatement(int year, int week, 
                int? truckid = null, string fuelCsv = null)
        {
            System.Console.WriteLine($"Creating settlements {year}/{week} {truckid?.ToString()}");
           
            Task.Run( async () => 
            {
                int[] weeks = new int[] { week };
                
                FuelChargeRepository fuelRepository = null;
                if (fuelCsv != null)
                    fuelRepository = new FuelChargeRepository(fuelCsv);

                SettlementRepository repository = new SettlementRepository();    
                List<SettlementHistory> settlements = await repository.GetSettlementsByWeekAsync(year, weeks);
                if (settlements.Count() > 0)
                {
                    SettlementWorkbookGenerator generator = new SettlementWorkbookGenerator(settlements, fuelRepository);
                    int[] trucks = (truckid == null) ? GetTrucks(settlements) : new int[] { (int)truckid };
                    foreach (int truck in trucks)
                    {
                        string file = generator.Generate(year, weeks, truck);
                        System.Console.WriteLine($"Created {file}.");
                    }
                }
                else
                {
                    System.Console.WriteLine($"No settlements found for the specified {year} and {string.Join(",", weeks)}");
                }
            }
            ).Wait();
        }

        private static int[] GetTrucks(List<SettlementHistory> settlements)
        {
            List<int> trucks = new List<int>();
            foreach (var s in settlements)
                trucks.AddRange(s.Credits.Select(c => c.TruckId).Where(t => t > 0));

            return trucks.Distinct().ToArray();
        }

        private static void ConsolidateSettlements()
        {
            System.Console.WriteLine("Consolidating settlements.");
            var task = Task.Run( async () => 
            {
                SettlementRepository repository = new SettlementRepository();
                await repository.ConsolidateSettlementsAsync();
            });
            task.Wait();
        }

        private static void GetReport()
        {
            System.Console.WriteLine("Generating report.");
            var task = Task.Run( async () => 
            {
                SettlementRepository repository = new SettlementRepository();
                RevenueReport report = new RevenueReport(repository);
                await report.GetTruckRevenueGroupBySettlementAsync();
            });
            task.Wait();            
        }

        private static void ShowUsage(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                System.Console.WriteLine($"[{i}]: {args[i]}");
            }
        }
    }
}
