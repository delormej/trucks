﻿/*
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
using jasondel.Tools;

namespace Trucks
{
    class Program
    {
        static void Main(string[] args)
        {
            ShowUsage(args);

            ParserConfiguration parserConfig = ParserConfiguration.Load();
            string convertApiKey = parserConfig.ZamzarKey;
            
            SettlementService settlementService = new SettlementService();
            SettlementOrchestrator orchestrator = new SettlementOrchestrator(
                parserConfig,
                settlementService);

            if (args.Length < 1)
                Process(orchestrator);
            else
            {
                string command = args[0].ToLower();
                Logger.Log($"Executing {command}");

                if (command == "uploaded")
                    orchestrator.ProcessUploadedAsync().Wait();
                else if (command == "downloaded")
                    orchestrator.ProcessLocal();
                else if (command == "update")
                    UpdateHeaders(settlementService);
                else if (command == "updateall")
                    settlementService.UpdateAll();                    
                else if (command == "report")
                    GetTruckRevenueReport();
                else if (command == "settlement")
                    CreateSettlements(settlementService, args);
                else if (command == "fixtemplate")
                    FixTemplate(args[1]);
                else if (command == "savefuel")
                    SaveFuelCharges(args[1]);
                else if (command == "get")
                    PrintSettlementHeader(args[1], args[2]);
                else if (command == "purge")
                    PurgeConvertedAsync(parserConfig.ZamzarKey).Wait();
            }
        }

        private static void CreateSettlements(SettlementService service, string[] args)
        {
            Logger.Log("Creating settlements...");
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
            Logger.Log("End to end process starting...");
            orchestrator.RunAsync().Wait();
            Logger.Log("Done!");
        }

        private static void GetTruckRevenueReport()
        {
            Logger.Log("Generating report.");
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
            Logger.Log("Fixing template cells C32, AA31");
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
            Logger.Log($"Saving {file} fuel charges to database.");
            FuelChargeRepository repository = new FuelChargeRepository();
            repository.SaveAsync(file).Wait();
            Logger.Log($"Saved {repository.Charges?.Count()} charge(s).");
        }

        private static void PrintSettlementHeader(string settlementId, string companyId)
        {
            Logger.Log($"Querying for {settlementId} in company: {companyId}");

            SettlementHistory settlement = null;
            Task.Run( async () => {
                SettlementRepository repo = new SettlementRepository();
                settlement = await repo.GetSettlementAsync(settlementId, companyId);
            }).Wait();

            if (settlement != null)
            {
                Logger.Log($"{settlement.id}, {settlement.SettlementDate}, {settlement.WeekNumber}");
            }
            else
            {
                Logger.Log("Settlement not found");
            }
        }

        private static async Task PurgeConvertedAsync(string convertApiKey)
        {
            Logger.Log($"Deleting all files on converter...");

            ExcelConverter converter = new ExcelConverter(convertApiKey);
            foreach (var result in await converter.QueryAllAsync())
            {
                await converter.DeleteAsync(result.id);
                Logger.Log($"Deleted {result.target_files[0].name}");
            }
        }

        private static void UpdateHeaders(SettlementService settlementService)
        {
            string company = Environment.GetEnvironmentVariable("TRUCKCOMPANY");
            string password = Environment.GetEnvironmentVariable("TRUCKPASSWORD");
            PantherClient panther = new PantherClient(company, password);                    
            settlementService.UpdateHeadersFromPanther(panther);            
        }

        private static void ShowUsage(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                Logger.Log($"[{i}]: {args[i]}");
            }
        }
    }
}
