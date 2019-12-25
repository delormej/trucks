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
                Process(company, password, convertApiKey);
            }
            else
            {
                string command = args[0].ToLower();
                if (command == "uploaded")
                    ProcessUploaded(company, convertApiKey);
                else if (command == "downloaded")
                    ProcessDownloaded(company);
                else if (command == "update")
                    UpdateSettlementHeaders(company, password);
            }
        }

        private static void ProcessDownloaded(string company)
        {
            System.Console.WriteLine("Processing local converted xlsx files.");

            List<SettlementHistory> settlements = SettlementHistoryParser.ParseLocalFiles(company);
            if (settlements.Count > 0)
            {
                System.Console.WriteLine($"Found {settlements.Count} to process.");
                Repository repository = new Repository();
                // repository.EnsureDatabaseAsync().Wait();
                repository.SaveSettlements(settlements, company);            
            }
            else
            {
                System.Console.WriteLine($"No settlements found for company {company}.");
            }
        }

        private static void ProcessUploaded(string company, string convertApiKey)
        {
            System.Console.WriteLine("Processing files already uploaded to converter.");

            ConvertedExcelFiles converted = new ConvertedExcelFiles(convertApiKey);
            converted.Process(company);
        }

        /// <summary>
        /// End-to-end task, downloads settlements from panther, converts, parses and persists to database.
        /// </summary>
        private static void Process(string company, string pantherPassword, string convertApiKey)
        {
            System.Console.WriteLine("Running end to end process");

            var task = Task.Run(async () => 
            {
                PantherClient panther = new PantherClient(company, pantherPassword);
                List<SettlementHistory> settlementsToConvert = await GetSettlementsToConvert(panther);
                ConversionOrchestrator orchestrator = new ConversionOrchestrator(settlementsToConvert, convertApiKey);
                await orchestrator.StartAsync();
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
                repository.SaveSettlements(settlementsToUpdate, company);
            });
            task.Wait();           
        }

        private static async Task<List<SettlementHistory>> GetSettlementsToConvert(PantherClient panther)
        {
            List<SettlementHistory> settlements = await panther.GetSettlementsAsync();

            Repository repository = new Repository();
            List<SettlementHistory> savedSettlements = await repository.GetSettlementsAsync();

            // Don't try to convert settlements we've already persisted.
            List<SettlementHistory> settlementsToConvert = settlements.Except(savedSettlements).ToList();            

            return settlementsToConvert;
        }

        private static void CreateSettlementStatements(List<RevenueDetail> details)
        {
            // select by truck
            var truckLoads = 
                from load in details
                group load by load.Truck into g 
                orderby g.Key
                select g;

            foreach (IGrouping<string, RevenueDetail> g in truckLoads)
            {
                Console.WriteLine("Group: " + g.Key);
                foreach (var load in g)
                {
                    Console.WriteLine("\t{0}, {1}", load.Truck, load.Date);
                }
            }

            using (ExcelWorkbook workbook = new ExcelWorkbook())
            {
                workbook.Open("Settlement.xlsx");
                //string value = workbook.GetCellValue("Week_27", "B1");
                workbook.UpdateCellValue("Week_27", "B1", "Johnny Cheekie");
                workbook.UpdateCellValue("Week_27", "C15", "100.91");
                
                //Console.WriteLine(value);
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
