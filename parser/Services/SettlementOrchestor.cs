using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace Trucks
{
    /// <summary> 
    /// Responsible for managing long running process end to end to download settlements, convert from
    /// xls to xlsx, parse and persist in backing store.  
    /// </summary>
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
        public void Run()
        {
            const int max = 10;
            System.Console.WriteLine("Running end to end...");

            EventWaitHandle ewh = new EventWaitHandle(false, EventResetMode.ManualReset);
            Finished += (o, e) => ewh.Set();
            
            _settlementService.DownloadMissingSettlements(_panther, max).Wait();
            ewh.WaitOne();
        }

        /// <summary>
        /// Downloads converted files from converter site, processes them as SettlementHistory
        /// and persists them to the backing store.
        /// <summary>
        public void ProcessUploaded()
        {
            ProcessConvertedAsync().Wait();
        }

        /// <summary>
        /// Resume process where settlements were downloaded as xlsx from converter.  Parses
        /// local xlsx files and persists to the backing store.
        /// </summary>
        public void ProcessDownloaded()
        {
            System.Console.WriteLine("Processing local converted xlsx files.");

            List<SettlementHistory> settlementHeaders = null; 
            List<SettlementHistory> downloadedSettlements = null;
            var getSettlementHeaders = Task.Run( async () => {
                settlementHeaders = await _panther.GetSettlementsAsync();
            });
            var parseLocalFiles = Task.Run( () =>
                downloadedSettlements = SettlementHistoryParser.ParseLocalFiles(_panther.Company));
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
                System.Console.WriteLine($"No settlements found for company {_panther.Company}.");
            }
        }
        
        /// <summary>
        /// Internal async implementation of ProcessUploaded()
        /// Downloads converted files from converter site, processes them as SettlementHistory
        /// and persists them to the backing store.
        /// <summary>
        private async Task ProcessConvertedAsync()
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

        /// <summary>
        /// Invoked when a settlement has been converted to xlsx.
        /// </summary>
        private async Task ProcessConversionResultAsync(ZamzarResult result, SettlementHistory settlement)
        {
            string filename = result.target_files[0].name;
            
            if (!File.Exists(filename))
                filename = await DownloadFromConverter(result, settlement.CompanyId);
            
            if (filename == null)
            {
                System.Console.WriteLine($"Local file {filename} does not exist.");
                return;
            }

            SettlementRepository repository = new SettlementRepository();
            try 
            {
                await repository.SaveFileToDatabaseAsync(filename, settlement);
                System.Console.WriteLine($"Processed Settlement company: {settlement.CompanyId}, id: {settlement.SettlementId} {DateTime.Now} ");
                OnProcessed(settlement.SettlementId);
                await _excelConverter.DeleteAsync(result.target_files[0].id);                    
            }
            catch (ApplicationException e)
            {
                System.Console.WriteLine(e.Message);
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

        private bool HasUploads()
        {
            return _uploaded?.Count > 0;
        }

        /// <summary>
        /// Invoked when a new settlement has been downloaded from Panther.
        /// </summary>
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

        private void OnCheckForDownload(object state)
        {
            System.Console.WriteLine("Processing files already uploaded to converter.");
            ProcessUploaded();
        }

        /// <summary>
        /// Invoked when a settlement has been persisted to the backing store.
        /// </summary>
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
        /// Kicks off a timer, on elapsed it checks for downloads on excel converter service.
        /// </summary>
        private void SetCheckForDownload()
        {
            // Set a timer , on expiration of that timer check for available downloads.
            const int MINUTES_3 = 3 * 60 * 1000; 
            Timer timer = new Timer(OnCheckForDownload, null, MINUTES_3, Timeout.Infinite);
        }
    }
}