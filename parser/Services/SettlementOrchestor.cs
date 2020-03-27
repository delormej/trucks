using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Linq;
using jasondel.Tools;

namespace Trucks
{
    /// <summary> 
    /// Responsible for managing long running process end to end to download settlements, convert from
    /// xls to xlsx, parse and persist in backing store.  
    /// </summary>
    /// <note>
    /// No business logic should live here, merely "orchestration".
    /// </note>
    public class SettlementOrchestrator
    {
        public readonly string ConvertedDirectory = "xlsx";
        private SettlementService _settlementService;
        private ExcelConverter _excelConverter;
        private List<ConversionJob> _uploaded;
        private Timer _checkForDownloadTimer;
        public event EventHandler Finished;
        public event ConvertedEventHandler Converted;
        public delegate void ConvertedEventHandler(object sender, ConvertedEventArgs e);
        private readonly ParserConfiguration _config;

        public class ConvertedEventArgs : EventArgs
        {
            public string Filename;
            public ConvertedEventArgs(string filename)
            {
                this.Filename = filename;
            }
        }

        public SettlementOrchestrator(ParserConfiguration config, SettlementService settlementService)
        {
            _config = config;

            _uploaded = new List<ConversionJob>();
            _excelConverter = new ExcelConverter(_config.ZamzarKey);
            _settlementService = settlementService;
            _settlementService.OnNewSettlement += OnNewSettlement;
        }

        /// <summary>
        /// Downloads the latest settlements, converts XLS to XLSX, processes and persists to backing store.
        /// </summary>
        public async Task RunAsync()
        {
            const int MINUTES_10 = 10 * 60 * 1000; 
            Logger.Log("Running end to end...");

            EventWaitHandle ewh = new EventWaitHandle(false, EventResetMode.ManualReset);
            Finished += (o, e) => ewh.Set();
            
            await DownloadSettlementsAsync();

            if (!ewh.WaitOne(MINUTES_10))
            {
                Logger.Log("Timeout waiting for processing of uploads.  Not all were processed?");
                foreach (var upload in _uploaded)
                    Logger.Log($"\t{upload.Company}, {upload.SettlementId}");
            }
        }

        /// <summary>
        /// Downloads converted files from converter site, processes them as SettlementHistory
        /// and persists them to the backing store.
        /// <summary>
        public async Task ProcessUploadedAsync()
        {
            Logger.Log("Checking converter for files to download.");

            Directory.CreateDirectory(ConvertedDirectory);
            IEnumerable<ZamzarResult> results = await _excelConverter.QueryAllAsync();
            List<SettlementHistory> settlements = await ParseUploadedAsync(results);
            SaveLocalSettlements(settlements);
        }

        /// <summary>
        /// Resume process where settlements were downloaded as xlsx from converter.  Parses
        /// local xlsx files and persists to the backing store.
        /// </summary>
        public void ProcessLocal(string directory = null)
        {
            if (directory == null)
                directory = ConvertedDirectory;

            Logger.Log($"Processing local converted xlsx files in {directory}.");

            List<SettlementHistory> downloadedSettlements = 
                SettlementHistoryParser.ParseLocalFiles(directory);

            if (downloadedSettlements.Count == 0)
            {
                Logger.Log($"No local settlements found in {directory} to process.");
                return;
            }

            SaveLocalSettlements(downloadedSettlements);
        }

        /// <summary>
        /// Helper function to wrap the async process of downloading and parsing individual files asyncronously.
        /// </summary>
        private async Task<List<SettlementHistory>> ParseUploadedAsync(IEnumerable<ZamzarResult> results)
        {
            IEnumerable<Task<SettlementHistory>> parseQuery =
                from result in results select ParseResultAsync(result);
            IEnumerable<SettlementHistory> settlements = await Task.WhenAll(parseQuery.ToArray());
            return settlements.ToList();

            async Task<SettlementHistory> ParseResultAsync(ZamzarResult result)
            {
                try
                {
                    string filename = await DownloadFromConverterAsync(result);
                    SettlementHistory settlement = SettlementHistoryParser.Parse(filename);
                    return settlement;
                }
                catch (Exception e)
                {
                    Logger.Log($"Unable to process uploaded: {result.target_files[0].name}\n{e}");
                    return null;
                }
            }
        }

        /// <summary>
        /// Helper method to asyncronously download missing settlements from each configured panther company.
        /// </summary>
        private async Task DownloadSettlementsAsync()
        {
            IEnumerable<Task> pantherQuery =
                from config in _config.Panther select DownloadAsync(config);

            Task DownloadAsync(PantherConfiguration config)
            {
                try
                {
                    PantherClient panther = new PantherClient(config.Company, config.Password);
                    return _settlementService.DownloadMissingSettlements(panther);   
                }
                catch (Exception e)
                {
                    Logger.Log($"Unable to download missing settlements for {config.Company}\n:{e}");
                    return null;
                }
            }
            
            await Task.WhenAll(pantherQuery.ToArray());
        }

        /// <summary>
        /// Downloads converted files from converter site and raised the OnConverted event
        /// for each file downloaded.
        /// <summary>
        private async Task<string> DownloadFromConverterAsync(ZamzarResult result)
        {
            if (result.target_files.Length == 0)
                throw new ApplicationException($"Unable to find a file for result: {result}");
            
            int fileId = result.target_files[0].id;
            SettlementFile file = SettlementFile.FromFilename(result.target_files[0].name);
            string filename = Path.Combine(ConvertedDirectory, file.Filename);
            
            // Abort if we've already downladed.
            if (!File.Exists(filename))
            {
                if (await _excelConverter.DownloadAsync(fileId, filename))
                    Logger.Log($"Downloaded: {filename}");
                else
                    Logger.Log($"Unable to download {filename}");
            }

            return filename;
        }    

        private void SaveLocalSettlements(List<SettlementHistory> settlements)
        {
            LoadSettlements(settlements.GetCompanies());
            List<SettlementHistory> mergedSettlements = _settlementService.MergeHeaders(settlements);

            if (settlements.Count == 0)
            {
                Logger.Log("No settlements to save.");
                return;
            }

            SettlementRepository repository = new SettlementRepository();
            repository.SaveSettlements(settlements);
            Logger.Log($"Saved {settlements.Count} settlements.");

            List<DriverSettlement> driverSettlements = DriverSettlementFactory
                .CreateDriverSettlements(settlements);
            repository.SaveDriverSettlements(driverSettlements);
            Logger.Log($"Saved {driverSettlements.Count} driver settlement(s).");
            
            OnFinished();
        }

        private void LoadSettlements(int[] companies)
        {
            List<Task> tasks = new List<Task>();
            foreach (int companyId in companies)
            {
                string company = companyId.ToString();
                if (company == null)
                    continue;
                
                var config = _config.Panther.Where(c => c.Company == company).FirstOrDefault();
                PantherClient panther = new PantherClient(company, config.Password);
                tasks.Add(_settlementService.LoadSettlementsAsync(panther));
            }
            if (tasks.Count > 0) 
                Task.WhenAll(tasks);            
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
                e.Job.Result = await _excelConverter.UploadAsync(e.Job.SourceXls);
                _uploaded.Add(e.Job);
                
                Logger.Log($"New Settlement uploaded for conversion: {e.Job.SettlementId}");

                SetCheckForDownload();
            }).Wait();
        }

        /// <summary> 
        /// Kicks off a timer, on elapsed it checks for downloads on excel converter service.
        /// </summary>
        private void SetCheckForDownload()
        {
            const int MINUTES_3 = 3 * 60 * 1000; 
            // Set a timer , on expiration of that timer check for available downloads.
            if (_checkForDownloadTimer == null)
                _checkForDownloadTimer = new Timer(OnCheckForDownload, null, MINUTES_3, Timeout.Infinite);
            else
            // Change to 3 minutes from the last time this is called.
                _checkForDownloadTimer.Change(MINUTES_3, Timeout.Infinite);
        }

        private void OnCheckForDownload(object state)
        {
            Logger.Log("Processing files already uploaded to converter.");
            ProcessUploadedAsync().Wait();
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
            if (!HasUploads())
                OnFinished();
        }

        private void OnFinished()
        {
            if(Finished != null)
                Finished(this, null);            
        }
    }
}