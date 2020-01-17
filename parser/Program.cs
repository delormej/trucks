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
                        CreateSettlementStatement(year, new int[] { week }, truck);
                    else
                        CreateSettlementStatement(year, new int[] { week });
                }
                else if (command == "year")
                {
                    CreateSettlementsForYear(int.Parse(args[1]));
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

        private static void CreateSettlementsForYear(int year)
        {
            int[] weeks = Enumerable.Range(1, 52).ToArray();
            CreateSettlementStatement(year, weeks);            
        }

        private static void CreateSettlementStatement(int year, int[] weeks, int? truckid = null)
        {
            System.Console.WriteLine($"Creating settlements {year}/{weeks[0]} {truckid?.ToString()}");
           
            Task.Run( async () => 
            {   
                FuelChargeRepository fuelRepository = new FuelChargeRepository();
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
                    }
                }
                else
                {
                    System.Console.WriteLine($"No settlements found for the specified {year} and {string.Join(",", weeks)}");
                }
            }
            ).Wait();
        }

        private static IEnumerable<SettlementHistory> FilterSettlementsByTruck(
                IEnumerable<SettlementHistory> settlements, int truckid)
        {
            return settlements.Where(
                        s => s.Credits.Where(c => c.TruckId == truckid).Count() > 0
                    );
        }

        private static int[] GetTrucks(List<SettlementHistory> settlements)
        {
            List<int> trucks = new List<int>();
            foreach (var s in settlements)
                trucks.AddRange(s.Credits.Select(c => c.TruckId).Where(t => t > 0));

            return trucks.Distinct().ToArray();
        }

        private static IEnumerable<string> GetDrivers(List<SettlementHistory> settlements, int? truckid)
        {
            IEnumerable<string> drivers = settlements.SelectMany(s => 
                    s.Credits.Where(c => truckid != null ? c.TruckId == truckid : true)
                    .Select(c => c.Driver)).Distinct();                
            
            return drivers;
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
            IEnumerable<FuelCharge> charges = repository.Load(file);
            repository.EnsureDatabaseAsync().Wait();
            var task = Task.Run(() => repository.SaveAsync(charges));
            task.Wait();
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
