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
        public void Process(string company)
        {            
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
                    tasks.Add(ProcessResultAsync(result, company));
            }
            Task.WaitAll(tasks.ToArray());
        }

        private bool AlreadySaved(ZamzarResult result, List<SettlementHistory> settlements)
        {
            string settlementId = SettlementHistoryParser.GetSettlementIdFromFile(result.target_files[0].name);
            bool exists = (settlements.Where(s => s.SettlementId == settlementId).Count() > 0);
            return exists;
        }            

        private async Task ProcessResultAsync(ZamzarResult result, string company)
        {
            string filename = result.target_files[0].name;
            if (!File.Exists(filename))
                filename = await DownloadFromConverter(converter, result, company);
            if (filename != null)
                SaveFileToDatabase(filename, company);
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

        private void SaveFileToDatabase(string filename, string company)
        {
            SettlementHistory settlement = SettlementHistoryParser.Parse(filename);
            settlement.CompanyId = int.Parse(company);
            repository.SaveSettlementHistoryAsync(settlement).Wait();
            System.Console.WriteLine($"Saved {settlement.SettlementId} to db.");                  
        }
    }
}