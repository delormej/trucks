using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Trucks
{
    public class ConvertedExcelFiles
    {
        ExcelConverter converter;
         Repository repository;

        public ConvertedExcelFiles(string convertApiKey)
        {
            converter = new ExcelConverter(convertApiKey);
            repository = new Repository();
        }

        /// <summary>
        /// Downloads converted files from converter site, processes them as SettlementHistory
        /// and persists them to the database.
        /// <summary>
        public void Process(PantherClient pantherClient)
        {            
            Repository repository = new Repository();

            var getConverterResults = converter.QueryAllAsync();
            var getSavedSettlements = repository.GetSettlementsAsync();
            var getSettlementHeaders = pantherClient.GetSettlementsAsync();
            Task.WaitAll(getConverterResults, getSavedSettlements, getSettlementHeaders);

            IEnumerable<ZamzarResult> results = getConverterResults.Result;
            List<SettlementHistory> savedSettlements = getSavedSettlements.Result;
            List<SettlementHistory> settlementHeaders = getSettlementHeaders.Result;

            List<Task> tasks = new List<Task>();
            foreach (ZamzarResult result in results)
            {
                if (!AlreadySaved(result, savedSettlements))
                {
                    string settlementId = GetSettlementId(result);
                    SettlementHistory settlement = settlementHeaders.Where(s => s.SettlementId == settlementId).FirstOrDefault();
                    if (settlement != null)
                        tasks.Add(ProcessResultAsync(result, settlement));
                    else
                        System.Console.WriteLine($"SettlementId {settlementId} not found.");
                }
            }
            Task.WaitAll(tasks.ToArray());
        }

        private bool AlreadySaved(ZamzarResult result, List<SettlementHistory> settlements)
        {
            string settlementId = GetSettlementId(result);
            bool exists = (settlements.Where(s => s.SettlementId == settlementId).Count() > 0);
            return exists;
        }            

        private string GetSettlementId(ZamzarResult result)
        {
            return SettlementHistoryParser.GetSettlementIdFromFile(
                result.target_files[0].name);
        }

        private async Task ProcessResultAsync(ZamzarResult result, SettlementHistory settlement)
        {
            string filename = result.target_files[0].name;
            if (!File.Exists(filename))
                filename = await DownloadFromConverter(converter, result, 
                    settlement.CompanyId.ToString());
            if (filename != null)
                SaveFileToDatabase(filename, settlement);
        }

        private async Task<string> DownloadFromConverter(ExcelConverter converter, ZamzarResult result, string company)
        {
            if (result.target_files.Length == 0)
                throw new ApplicationException($"Unable to find a file for result: {result}");
            
            string filename = Path.Combine(company, result.target_files[0].name);
            int fileId = result.target_files[0].id;
            if (await converter.DownloadAsync(fileId, filename))
            {
                System.Console.WriteLine($"Downloaded: {filename}");
                return filename;
            }
            else
            {
                return null;
            }            
        }

        private void SaveFileToDatabase(string filename, SettlementHistory settlement)
        {
            SettlementHistory parsedSettlement = SettlementHistoryParser.Parse(filename);
            settlement.Credits = parsedSettlement.Credits;
            settlement.Deductions = parsedSettlement.Deductions;

            repository.SaveSettlementHistoryAsync(settlement).Wait();
            System.Console.WriteLine($"Saved {settlement.SettlementId} to db.");                  
        }
    }
}