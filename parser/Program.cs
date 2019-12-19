﻿using System;
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
                       
            var task2 = Task.Run( () => ProcessUploaded(company, convertApiKey) );
            task2.Wait();
            return;

            // var task = ProcessAsync(company, password, convertApiKey);
            // task.Wait();
            // System.Console.WriteLine("Done");
            // return;
           
        }

        // Processes files ready for download from conversion.
        private static async Task ProcessUploaded(string company, string convertApiKey)
        {
            ExcelConverter converter = new ExcelConverter(convertApiKey);
            Repository repository = new Repository();
            IEnumerable<ZamzarResult> results = await converter.QueryAllAsync();
            List<Task> tasks = new List<Task>();

            foreach (ZamzarResult result in results)
                tasks.Add(ProcessResultAsync(result));

            async Task ProcessResultAsync(ZamzarResult result)
            {
                string filename = result.target_files[0].name;
                if (!File.Exists(filename))
                    filename = await Download(converter, result);
                SettlementHistory settlement = Parse(filename);
                settlement.CompanyId = int.Parse(company);
                
                // Store in cosmosdb
                await repository.SaveSettlementHistoryAsync(settlement);
                System.Console.WriteLine($"Saved {settlement.SettlementId} to db.");            
            }

            Task.WaitAll(tasks.ToArray());
        }


        private static async Task<string> Download(ExcelConverter converter, ZamzarResult result)
        {
            if (result.target_files.Length == 0)
                throw new ApplicationException($"Unable to find a file for result: {result}");
            
            string filename = result.target_files[0].name;
            int fileId = result.target_files[0].id;
            await converter.DownloadAsync(fileId, filename);
            System.Console.WriteLine($"Downloaded: {filename}");

            return filename;
        }

        private static SettlementHistory Parse(string filename)
        {
            SettlementHistoryParser parser = new SettlementHistoryParser(
                filename, GetSettlementIdFromFile(filename));
            SettlementHistory settlement = parser.Parse();
            System.Console.WriteLine($"Parsed: {filename} with {settlement.Credits.Count} credits.");            
            
            return settlement;
        }

        private static async Task ProcessAsync(string company, string pantherPassword, string convertApiKey)
        {
            List<SettlementHistory> settlements = await DownloadSettlementsAsync(company, pantherPassword); // ReadLocalFiles(company); // 
            ConversionOrchestrator orchestrator = new ConversionOrchestrator(settlements, convertApiKey);
            await orchestrator.StartAsync();
        }

        private static List<SettlementHistory> ReadLocalFiles(string company)
        {
            List<SettlementHistory> settlements = new List<SettlementHistory>();
            string[] settlementFiles = Directory.GetFiles(company);

            foreach (var file in settlementFiles)
            {
                SettlementHistory settlement = new SettlementHistory();
                settlement.CompanyId = int.Parse(company);
                settlement.SettlementId = GetSettlementIdFromFile(file);
                settlements.Add(settlement);
            }

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

        private static async Task<List<SettlementHistory>> DownloadSettlementsAsync(string company, string password)
        {
            PantherClient client = new PantherClient();
            
            bool loggedIn = await client.LoginAsync(company, password);
            if (!loggedIn)
                throw new ApplicationException("Unable to login with credentials.");
            
            string payrollHistHtml = await client.GetPayrollHistAsync();
            
            PayrollHistHtmlParser parser = new PayrollHistHtmlParser(company);
            List<SettlementHistory> settlements = parser.Parse(payrollHistHtml);
            IEnumerable<SettlementHistory> workingSettlements = 
                settlements.OrderByDescending(s => s.SettlementDate).Take(10);

            foreach (SettlementHistory settlement in workingSettlements)
            {
                string xls = await client.DownloadSettlementReportAsync(company, settlement.SettlementId);
                System.Console.WriteLine($"Downloaded {settlement.SettlementId}: {xls}");
            }
            
            return workingSettlements.ToList();
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
