using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Trucks
{
    public class SettlementOrchestrator
    {
        private SettlementService _settlementService;
        private ExcelConverter _excelConverter;
        private PantherClient _panther;
        private List<ConversionJob> _uploaded;
        public event EventHandler Finished;

        public SettlementOrchestrator(SettlementService settlementService, ExcelConverter excelConverter, PantherClient panther)
        {
            _uploaded = new List<ConversionJob>();
            _excelConverter = excelConverter;
            _panther = panther;
            _settlementService = settlementService;
            _settlementService.OnNewSettlement += OnNewSettlement;
        }

        /// <summary>
        /// Downloads the latest settlements, converts XLS to XLSX, processes and persists to backing store.
        /// </summary>
        public async Task StartAsync()
        {
            // #pragma warning disable CS4014
            // This is an anti-pattern, figure out a better way to signal and forget.
            // Biggest issue here is what happens if an exception is thrown in the method
            // below, it will not catch.
            System.Console.WriteLine("Downloading settlements from panther and uploading to conversion service.");
            int max = 10;
            await _settlementService.DownloadMissingSettlements(_panther, max);
        }

        /// <summary>
        /// Downloads converted files from converter site, processes them as SettlementHistory
        /// and persists them to the database.
        /// <summary>
        public async Task ProcessConvertedAsync()
        {
            IEnumerable<ZamzarResult> results = await _excelConverter.QueryAllAsync();
            List<SettlementHistory> settlementHeaders = await _panther.GetSettlementsAsync();

            List<Task> tasks = new List<Task>();
            foreach (ZamzarResult result in results)
            {
                string settlementId = GetSettlementId(result);
                SettlementHistory settlementHeader = settlementHeaders.FindSettlementById(settlementId);                    
                if (settlementHeader != null)
                    tasks.Add(ProcessConversionResultAsync(result, settlementHeader));
                else
                    System.Console.WriteLine($"SettlementId {settlementId} not found on panther.");
            }
            await Task.WhenAll(tasks);

            string GetSettlementId(ZamzarResult result)
            {
                return SettlementHistoryParser.GetSettlementIdFromFile(
                    result.target_files[0].name);
            }       
        }

        public bool HasUploads()
        {
            return _uploaded?.Count > 0;
        }

        private void OnNewSettlement(object state, NewSettlementEventArgs e)
        {
            Task.Run(async () => 
            {
                ConversionJob job = e.Job;
                job.Result = await _excelConverter.UploadAsync(job.SourceXls);
                _uploaded.Add(e.Job);
                
                System.Console.WriteLine($"New Settlement uploaded for conversion: {e.Job.SettlementId}");

                SetCheckForDownload();
            }).Wait();
        }

        private void SetCheckForDownload()
        {
            // Set a timer , on expiration of that timer check for available downloads.
            const int MINUTES_3 = 3 * 60 * 1000; 
            Timer timer = new Timer(OnCheckForDownload, null, MINUTES_3, Timeout.Infinite);
        }

        private void OnCheckForDownload(object state)
        {
            System.Console.WriteLine("Processing files already uploaded to converter.");
            ProcessConvertedAsync().Wait();
        }

        private void OnProcessed(string settlementId)
        {
            // Remove from the queue.
            var job = _uploaded.Find(s => s.SettlementId == settlementId);
            if (job != null)
                _uploaded.Remove(job);
            if (!HasUploads() && Finished != null)
                Finished(this, null);
        }

        /// <summary>
        /// Invoked when a settlement has been converted to xlsx.
        /// </summary>
        private async Task ProcessConversionResultAsync(ZamzarResult result, SettlementHistory settlement)
        {
            string filename = result.target_files[0].name;
            
            if (!File.Exists(filename))
                filename = await DownloadFromConverter(result, settlement.CompanyId);
            
            if (filename != null)
            {
                SettlementRepository repository = new SettlementRepository();
                if (repository.SaveFileToDatabase(filename, settlement))
                {
                    OnProcessed(settlement.SettlementId);
                    await _excelConverter.DeleteAsync(result.target_files[0].id);
                    System.Console.WriteLine($"Processed Settlement company: {settlement.CompanyId}, id: {settlement.SettlementId} {DateTime.Now} ");
                }
            }
        }

        private async Task<string> DownloadFromConverter(ZamzarResult result, int companyId)
        {
            string company = companyId.ToString();
            if (result.target_files.Length == 0)
                throw new ApplicationException($"Unable to find a file for result: {result}");
            
            string filename = Path.Combine(company, result.target_files[0].name);
            int fileId = result.target_files[0].id;
            if (await _excelConverter.DownloadAsync(fileId, filename))
            {
                System.Console.WriteLine($"Downloaded: {filename}");
                return filename;
            }
            else
            {
                return null;
            }            
        }
    }
}