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

            SettlementService settlementService = new SettlementService();

            if (string.IsNullOrWhiteSpace(company) || 
                string.IsNullOrWhiteSpace(password) || 
                string.IsNullOrWhiteSpace(convertApiKey))
            {
                System.Console.WriteLine("Must set TRUCKCOMPANY, TRUCKPASSWORD, ZAMZARKEY env variables.");
                return;
            }

            if (args.Length < 1)
            {
                DownloadAndConvert(settlementService, company, password, convertApiKey);
            }
            else
            {
                string command = args[0].ToLower();
                if (command == "uploaded")
                    ProcessUploaded(company, password, convertApiKey);
                else if (command == "downloaded")
                    ProcessDownloaded(company, password, convertApiKey);
                else if (command == "update")
                    settlementService.UpdateHeadersFromPanther(company, password);
                else if (command == "updateall")
                    settlementService.UpdateAll();                    
                else if (command == "report")
                    GetReport();
                else if (command == "settlement")
                {
                    int year = int.Parse(args[1]);
                    int week = int.Parse(args[2]);
                    int truck;
                    if (args.Length > 3 && int.TryParse(args[3], out truck))
                        settlementService.CreateSettlementStatement(year, new int[] { week }, truck);
                    else
                        settlementService.CreateSettlementStatement(year, new int[] { week });
                }
                else if (command == "year")
                {
                    settlementService.CreateSettlementsForYear(int.Parse(args[1]));
                }
                else if (command == "fixtemplate")
                {
                    FixTemplate(args[1]);
                }
                else if (command == "savefuel")
                {
                    SaveFuelCharges(args[1]);
                }       
                else if (command == "get")
                {
                    PrintSettlementHeader(args[1], args[2]);
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
        private static void DownloadAndConvert(SettlementService settlementService, string company, string pantherPassword, string convertApiKey, int max = 10)
        {
            System.Console.WriteLine("Downloading settlements from panther and uploading to conversion service.");

            var task = Task.Run(async () => 
            {
                List<SettlementHistory> settlementsToConvert = 
                    await settlementService.DownloadMissingSettlements(company, pantherPassword);
                
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

        private static int[] GetTrucks(List<SettlementHistory> settlements)
        {
            List<int> trucks = new List<int>();
            foreach (var s in settlements)
                trucks.AddRange(s.Credits.Select(c => c.TruckId).Where(t => t > 0));

            return trucks.Distinct().ToArray();
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

        private static void FixTemplate(string template)
        {
            System.Console.WriteLine("Fixing template cells C32, AA31");
            const string formula = "IF(B27>0,(0.04*B27)+20,0)"; 
            using (var wb = new SettlementHistoryWorkbook(template))
            {
                for (int i = 1; i <= 52; i++)
                {
                    string sheet = $"Week_{i}";
                    wb.UpdateCellValue(sheet, "C32", "0");
                    wb.UpdateCellFormula(sheet, "AA31", formula);
                }
                wb.Save();
            }
        }

        private static void SaveFuelCharges(string file)
        {
            System.Console.WriteLine($"Saving {file} fuel charges to database.");
            FuelChargeRepository repository = new FuelChargeRepository();

            var loadTask = repository.LoadAsync(file);
            Task.Run( async () => {
                await repository.EnsureDatabaseAsync();
                await loadTask;
                repository.SaveAsync(repository.Charges);
                System.Console.WriteLine($"Saved {repository.Charges?.Count()} charge(s).");
            }).Wait();
        }

        private static void PrintSettlementHeader(string settlementId, string companyId)
        {
            System.Console.WriteLine($"Querying for {settlementId} in company: {companyId}");

            SettlementHistory settlement = null;
            Task.Run( async () => {
                SettlementRepository repo = new SettlementRepository();
                settlement = await repo.GetSettlementAsync(settlementId, companyId);
            }).Wait();

            if (settlement != null)
            {
                System.Console.WriteLine($"{settlement.id}, {settlement.SettlementDate}, {settlement.WeekNumber}");
            }
            else
            {
                System.Console.WriteLine("Settlement not found");
            }
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
