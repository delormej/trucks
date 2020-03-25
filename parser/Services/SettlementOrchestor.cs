using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Linq;

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
            System.Console.WriteLine("Running end to end...");

            EventWaitHandle ewh = new EventWaitHandle(false, EventResetMode.ManualReset);
            Finished += (o, e) => ewh.Set();
            
            await DownloadSettlementsAsync();

            if (!ewh.WaitOne(MINUTES_10))
            {
                System.Console.WriteLine("Timeout waiting for processing of uploads.  Not all were processed?");
                foreach (var upload in _uploaded)
                    System.Console.WriteLine($"\t{upload.Company}, {upload.SettlementId}");
            }
        }

        /// <summary>
        /// Downloads converted files from converter site, processes them as SettlementHistory
        /// and persists them to the backing store.
        /// <summary>
        public async Task ProcessUploadedAsync()
        {
            System.Console.WriteLine("Checking converter for files to download.");
            Directory.CreateDirectory(ConvertedDirectory);
            IEnumerable<ZamzarResult> results = await _excelConverter.QueryAllAsync();
            List<SettlementHistory> mergedSettlements = new List<SettlementHistory>();

            foreach (ZamzarResult result in results)
            {
                try
                {
                    string filename = await DownloadFromConverterAsync(result);
                    SettlementHistory settlement = 
                        MergeHeader(SettlementHistoryParser.Parse(filename));
                    mergedSettlements.Add(settlement);
                }
                catch (Exception e)
                {
                    System.Console.WriteLine($"Unable to process uploaded: {result.target_files[0].name}\n{e}");
                }
            }

            SaveSettlements(mergedSettlements);
        }

        /// <summary>
        /// Resume process where settlements were downloaded as xlsx from converter.  Parses
        /// local xlsx files and persists to the backing store.
        /// </summary>
        public void ProcessLocal(string directory = null)
        {
            System.Console.WriteLine("Processing local converted xlsx files.");
            if (directory == null)
                directory = ConvertedDirectory;

            List<SettlementHistory> downloadedSettlements = 
                SettlementHistoryParser.ParseLocalFiles(directory);

            if (downloadedSettlements.Count == 0)
            {
                System.Console.WriteLine($"No local settlements found to process.");
                return;
            }

            List<SettlementHistory> mergedSettlements = new List<SettlementHistory>();

            foreach (var settlement in downloadedSettlements)
                mergedSettlements.Add(MergeHeader(settlement));

            SaveSettlements(mergedSettlements);
        }

        private async Task DownloadSettlementsAsync()
        {
            List<Task> tasks = new List<Task>();

            foreach (var pantherConfig in _config.Panther)
            {
                tasks.Add(Task.Run( async () => 
                {
                    try
                    {
                        PantherClient panther = new PantherClient(pantherConfig.Company, pantherConfig.Password);
                        await _settlementService.DownloadMissingSettlements(panther);   
                    }
                    catch (Exception e)
                    {
                        System.Console.WriteLine($"Unable to download missing settlements\n:{e}");
                    }
                }));
            }

            await Task.WhenAll(tasks);
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
                    System.Console.WriteLine($"Downloaded: {filename}");
                else
                    System.Console.WriteLine($"Unable to download {filename}");
            }

            return filename;
        }        

        private void SaveSettlements(List<SettlementHistory> settlements)
        {
            if (settlements.Count > 0)
            {
                SettlementRepository repository = new SettlementRepository();
                repository.SaveSettlements(settlements);
            
                System.Console.WriteLine($"Saved {settlements.Count} settlements.");
            }

            OnFinished();
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
                
                System.Console.WriteLine($"New Settlement uploaded for conversion: {e.Job.SettlementId}");

                SetCheckForDownload();
            }).Wait();
        }

        private void OnCheckForDownload(object state)
        {
            System.Console.WriteLine("Processing files already uploaded to converter.");
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

        private object _pantherLock = new Object();
        private SettlementHistory GetHeader(SettlementHistory settlement)
        {
            SettlementHistory header = _settlementService.SettlementHeaders?
                    .FindSettlementById(settlement.SettlementId);
            
            if (header == null)
            {
                // Get the additional header context from panther.
                if (!_settlementService.SettlementHeaders.ContainsCompany(settlement.CompanyId))
                {
                    System.Console.WriteLine($"Settlement {settlement.SettlementId} not found, trying to load from Panther.");
                    
                    LoadFromPanther(settlement.CompanyId.ToString());
                    header = _settlementService.SettlementHeaders?
                        .FindSettlementById(settlement.SettlementId);   
                }
            }
            
            return header;
        }

        private void LoadFromPanther(string company)
        {
            string password = _config.GetPantherPassword(company);

            lock(_pantherLock)
            {
                PantherClient panther = new PantherClient(company, password);
                panther.GetSettlementsAsync().ContinueWith( (t) => 
                    _settlementService.SettlementHeaders.AddRange(t.Result)
                ).Wait();
            }
        }

        private SettlementHistory MergeHeader(SettlementHistory settlement)
        {
            SettlementHistory header = GetHeader(settlement);
            if (header == null)
                throw new ApplicationException($"Cannot find header for {settlement.SettlementId}");

            header.Credits = settlement.Credits;
            header.Deductions = settlement.Deductions;

            return header;            
        }
    }
}