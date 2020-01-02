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
                    Repository repository = new Repository();
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

                Repository repository = new Repository();
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
                Repository repository = new Repository();
                List<SettlementHistory> savedSettlements = await repository.GetSettlementsAsync();
                repository.SaveSettlements(savedSettlements);
            });
            task.Wait();                       
        }

        private static async Task<List<SettlementHistory>> GetSettlementsToConvert(PantherClient panther, int max = 10)
        {
            List<SettlementHistory> settlements = await panther.GetSettlementsAsync();

            Repository repository = new Repository();
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

        private static void CreateSettlementStatement(DateTime settlementDate, int truckid)
        {
        }

        private static void ConsolidateSettlements()
        {
            System.Console.WriteLine("Consolidating settlements.");
            var task = Task.Run( async () => 
            {
                Repository repository = new Repository();
                await repository.ConsolidateSettlementsAsync();
            });
            task.Wait();
        }

        private static void GetReport()
        {
            System.Console.WriteLine("Generating report.");
            var task = Task.Run( async () => 
            {
                Repository repository = new Repository();
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
