using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
            
            PantherClient panther = new PantherClient(company, password);
            ExcelConverter converter = new ExcelConverter(convertApiKey);
            SettlementService settlementService = new SettlementService();
            SettlementOrchestrator orchestrator = new SettlementOrchestrator(
                settlementService,
                converter,
                panther);

            if (args.Length < 1)
                Process(orchestrator);
            else
            {
                string command = args[0].ToLower();
                if (command == "uploaded")
                    ProcessUploaded(orchestrator);
                else if (command == "downloaded")
                    ProcessDownloaded(panther, convertApiKey);
                else if (command == "update")
                    settlementService.UpdateHeadersFromPanther(panther);
                else if (command == "updateall")
                    settlementService.UpdateAll();                    
                else if (command == "report")
                    GetTruckRevenueReport();
                else if (command == "settlement")
                    CreateSettlements(settlementService, args);
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

        private static void CreateSettlements(SettlementService service, string[] args)
        {
            System.Console.WriteLine("Creating settlements...");
            Task.Run(() =>
                service.CreateSettlementsAsync(GetOptions(args))
            ).Wait();
        }

        private static CreateSettlementOptions GetOptions(string[] args)
        {
            CreateSettlementOptions options = new CreateSettlementOptions();
            if (args.Length > 1)
            {
                int year = int.Parse(args[1]);
                options.Year = year;
            }
            if (args.Length > 2)
            {
                int week = int.Parse(args[2]);
                options.Weeks = new int[] { week };
            }
            if (args.Length > 3)
            {
                int truck;
                if (int.TryParse(args[3], out truck)) 
                    options.TruckId = truck;
            }

            return options;
        }

        private static void ProcessDownloaded(PantherClient panther, string convertApiKey)
        {
            System.Console.WriteLine("Processing local converted xlsx files.");

            List<SettlementHistory> settlementHeaders = null; 
            List<SettlementHistory> downloadedSettlements = null;
            var getSettlementHeaders = Task.Run( async () => {
                settlementHeaders = await panther.GetSettlementsAsync();
            });
            var parseLocalFiles = Task.Run( () =>
                downloadedSettlements = SettlementHistoryParser.ParseLocalFiles(panther.Company));
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
                System.Console.WriteLine($"No settlements found for company {panther.Company}.");
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

        private static void Process(SettlementOrchestrator orchestrator)
        {
            // Could have 'n' orchestators, 1 for each panther company???
            System.Console.WriteLine("End to end process starting...");
            orchestrator.StartAsync().Wait();
            System.Console.WriteLine("Done!");
        }

        private static void ProcessUploaded(SettlementOrchestrator orchestrator)
        {
            System.Console.WriteLine("Processing files already uploaded to converter.");
            orchestrator.ProcessConvertedAsync().Wait();
        }

        private static void GetTruckRevenueReport()
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
                    wb.UpdateCellValue(sheet, "C3", "");
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
            repository.SaveAsync(file).Wait();
            System.Console.WriteLine($"Saved {repository.Charges?.Count()} charge(s).");
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
