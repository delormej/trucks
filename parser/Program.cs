/*
Next big things:
1) Orchestrator
- Orchestrator should be responsible for:
    1) Run()
        - Starts end to end BLOCKING process.
        - Uses EventWaitHandle to receive finish signal.
    2) ProcessUploaded()
        - Resumes by checking the excel conversion service for settlements to download
    3) ProcessDownloaded()
        - Resume by checking local file system for files

- Orchestrator should be executable as a backend service.
- Orchestrator should be able to store resumable state in an Azure Queue/or other service.
- Orchestrator should be able to work on behalf of multiple companies configured... 
    maybe even multiple conversion keys for Excel Converter

2) DriverSettlement
- A driver settlement is a new backing object store that represents a driver's settlement
- It's a single document, but it can contain credits and deductions that are both pulled
    from Panther as well as manually entered.

3) Configuration provider should be used and passed to Orchestrator
    - Contains one or more companies
    - Contains one or more conversion keys
*/


using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Trucks
{
    class Program
    {
        static void Main(string[] args)
        {
            ShowUsage(args);

            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            ParserConfiguration parserConfig = new ParserConfiguration();
            config.Bind("parser", parserConfig);

            string company = Environment.GetEnvironmentVariable("TRUCKCOMPANY");
            string password = Environment.GetEnvironmentVariable("TRUCKPASSWORD");
            string convertApiKey = parserConfig.ZamzarKey;

            if (string.IsNullOrWhiteSpace(convertApiKey))
            {
                System.Console.WriteLine("Must set TRUCKCOMPANY, TRUCKPASSWORD, ZAMZARKEY env variables.");
                return;
            }
            
            PantherClient panther = new PantherClient(company, password);
            SettlementService settlementService = new SettlementService();
            SettlementOrchestrator orchestrator = new SettlementOrchestrator(
                config,
                settlementService,
                new ExcelConverter(convertApiKey));

            if (args.Length < 1)
                Process(orchestrator);
            else
            {
                string command = args[0].ToLower();
                System.Console.WriteLine($"Executing {command}");

                if (command == "uploaded")
                    orchestrator.ProcessUploadedAsync().Wait();
                else if (command == "downloaded")
                    orchestrator.ProcessLocal();
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
                else if (command == "purge")
                {
                    PurgeConvertedAsync(parserConfig.ZamzarKey).Wait();
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

        private static void Process(SettlementOrchestrator orchestrator)
        {
            // Could have 'n' orchestators, 1 for each panther company???
            System.Console.WriteLine("End to end process starting...");
            orchestrator.RunAsync().Wait();
            System.Console.WriteLine("Done!");
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

        private static async Task PurgeConvertedAsync(string convertApiKey)
        {
            System.Console.WriteLine($"Deleting all files on converter...");

            ExcelConverter converter = new ExcelConverter(convertApiKey);
            foreach (var result in await converter.QueryAllAsync())
            {
                await converter.DeleteAsync(result.id);
                System.Console.WriteLine($"Deleted {result.target_files[0].name}");
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
