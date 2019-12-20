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
            ShowUsage();

            string company = Environment.GetEnvironmentVariable("TRUCKCOMPANY");
            string password = Environment.GetEnvironmentVariable("TRUCKPASSWORD");
            string convertApiKey = Environment.GetEnvironmentVariable("ZAMZARKEY");

            if (args.Length <= 1)
            {
                Process(company, password, convertApiKey);
            }
            else
            {
                string command = args[1].ToLower();
                if (command == "uploaded")
                    ProcessUploaded(company, convertApiKey);
                else if (command == "downloaded")
                    ProcessDownloaded(company);
            }
        }

        private static void ProcessDownloaded(string company)
        {
            List<SettlementHistory> settlements = SettlementHistoryParser.ParseLocalFiles(company);
            Repository repository = new Repository();
            repository.EnsureDatabaseAsync().Wait();
            repository.SaveSettlements(settlements, company);            
        }

        private static void ProcessUploaded(string company, string convertApiKey)
        {
            ConvertedExcelFiles converted = new ConvertedExcelFiles(convertApiKey);
            converted.Process(company);
        }

        /// <summary>
        /// End-to-end task, downloads settlements from panther, converts, parses and persists to database.
        /// </summary>
        private static void Process(string company, string pantherPassword, string convertApiKey)
        {
            var task = Task.Run(async () => 
            {
                PantherClient panther = new PantherClient(company, pantherPassword);
                List<SettlementHistory> settlements = await panther.DownloadSettlementsAsync(); 
                ConversionOrchestrator orchestrator = new ConversionOrchestrator(settlements, convertApiKey);
                await orchestrator.StartAsync();
            });
            task.Wait();
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

        private static void ShowUsage()
        {
            
        }
    }
}
