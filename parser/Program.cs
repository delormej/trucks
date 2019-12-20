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
            string company = Environment.GetEnvironmentVariable("TRUCKCOMPANY") ?? (args.Length > 1 ? args[1] : "");
            string password = Environment.GetEnvironmentVariable("TRUCKPASSWORD") ?? (args.Length > 1 ? args[2] : "");
            string convertApiKey = Environment.GetEnvironmentVariable("ZAMZARKEY") ?? (args.Length > 3 ? args[3] : "");

            // Repository repository = new Repository();
            // repository.EnsureDatabaseAsync().Wait();

            List<SettlementHistory> settlements = ReadLocalFiles(company);
            SaveSettlements(settlements, company);

            //ProcessUploaded(company, convertApiKey);

            // var task = ProcessAsync(company, password, convertApiKey);
            // task.Wait();
            // System.Console.WriteLine("Done");
        }

        private static async Task ProcessAsync(string company, string pantherPassword, string convertApiKey)
        {
            PantherClient panther = new PantherClient(company, pantherPassword);
            List<SettlementHistory> settlements = await panther.DownloadSettlementsAsync(); 
            ConversionOrchestrator orchestrator = new ConversionOrchestrator(settlements, convertApiKey);
            await orchestrator.StartAsync();
        }

        // Processes files ready for download from conversion.
        private static void ProcessUploaded(string company, string convertApiKey)
        {
            ExcelConverter converter = new ExcelConverter(convertApiKey);
            Repository repository = new Repository();

            // Kick these two off async.
            var getConverterResults = converter.QueryAllAsync();
            var getSavedSettlements = repository.GetSettlementsAsync();
            Task.WaitAll(getConverterResults, getSavedSettlements);

            IEnumerable<ZamzarResult> results = getConverterResults.Result;
            List<SettlementHistory> savedSettlements = getSavedSettlements.Result;

            List<Task> tasks = new List<Task>();
            foreach (ZamzarResult result in results)
            {
                if (!AlreadySaved(result, savedSettlements))
                    tasks.Add(ProcessResultAsync(result));
            }
            Task.WaitAll(tasks.ToArray());

            async Task ProcessResultAsync(ZamzarResult result)
            {
                string filename = result.target_files[0].name;
                if (!File.Exists(filename))
                    filename = await Download(converter, result, company);
                SaveFileToDatabase(filename, company, repository);
            }
        }

        private static void SaveSettlements(List<SettlementHistory> settlements, string company)
        {
            Repository repository = new Repository();
            foreach (SettlementHistory settlement in settlements)
            {
                try
                {
                    Task task = Task.Run(() => repository.SaveSettlementHistoryAsync(settlement));
                    task.Wait();
                    if (task.Exception != null)
                        throw task.Exception;
                    System.Console.WriteLine($"Saved settlement id: {settlement.SettlementId}, now waiting a second.");
                    System.Threading.Thread.Sleep(100);
                }
                catch (Exception e)
                {
                    System.Console.WriteLine(
                        $"Error atempting to save settlement: {settlement.id} to database.\n\t{e.Message}");
                }
            }
        }

        private static void SaveFileToDatabase(string filename, string company, Repository repository)
        {
            SettlementHistory settlement = Parse(filename, company);
            repository.SaveSettlementHistoryAsync(settlement).Wait();
            System.Console.WriteLine($"Saved {settlement.SettlementId} to db.");                  
        }

        private static bool AlreadySaved(ZamzarResult result, List<SettlementHistory> settlements)
        {
            string settlementId = GetSettlementIdFromFile(result.target_files[0].name);
            bool exists = (settlements.Where(s => s.SettlementId == settlementId).Count() > 0);
            return exists;
        }

        private static async Task<string> Download(ExcelConverter converter, ZamzarResult result, string company)
        {
            if (result.target_files.Length == 0)
                throw new ApplicationException($"Unable to find a file for result: {result}");
            
            string filename = Path.Combine(company, result.target_files[0].name);
            int fileId = result.target_files[0].id;
            await converter.DownloadAsync(fileId, filename);
            System.Console.WriteLine($"Downloaded: {filename}");

            return filename;
        }

        private static SettlementHistory Parse(string filename, string company)
        {
            SettlementHistoryParser parser = new SettlementHistoryParser(
                filename, GetSettlementIdFromFile(filename));
            SettlementHistory settlement = parser.Parse();
            settlement.CompanyId = int.Parse(company);
            settlement.SettlementId = GetSettlementIdFromFile(filename);            
            System.Console.WriteLine($"Parsed: {filename} with {settlement.Credits.Count} credits.");            
            
            return settlement;
        }

        private static List<SettlementHistory> ReadLocalFiles(string company)
        {
            List<SettlementHistory> settlements = new List<SettlementHistory>();
            string[] settlementFiles = Directory.GetFiles(company, "*.xlsx");

            foreach (var filename in settlementFiles)
                settlements.Add(Parse(filename, company));

            return settlements;
        }

        private static string GetSettlementIdFromFile(string file)
        {
            string filename = Path.GetFileName(file);
            int i = filename.IndexOf(".xls");
            if (i <= 0)
                throw new ApplicationException($"Unable to get SettlmentId from filename: {file}");

            return filename.Substring(0, i);            
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
