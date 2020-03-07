using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Trucks
{
    public class SettlementOrchestrator
    {
        private SettlementService _settlementService;
        private ExcelConverter _excelConverter;
        private ConvertedExcelFiles _converted;
        private PantherClient _panther;
        private List<ConversionJob> _uploaded;
        public event EventHandler Finished;

        public SettlementOrchestrator(SettlementService settlementService, ExcelConverter excelConverter, PantherClient panther)
        {
            _uploaded = new List<ConversionJob>();
            _excelConverter = excelConverter;
            _converted = new ConvertedExcelFiles(_excelConverter);
            _panther = panther;
            _settlementService = settlementService;
            _settlementService.NewSettlement += OnNewSettlement;
            _converted.ProcessedSettlement += OnProcessed;
        }

        public void Start()
        {
            #pragma warning disable CS4014
            // This is an anti-pattern, figure out a better way to signal and forget.
            // Biggest issue here is what happens if an exception is thrown in the method
            // below, it will not catch.
            _settlementService.StartConversion(_panther, _excelConverter);
        }

        public bool HasUploads()
        {
            return _uploaded?.Count > 0;
        }

        private void OnNewSettlement(object state, NewSettlementEventArgs e)
        {
            System.Console.WriteLine($"New Settlement uploaded for conversion: {e.Job.SettlementId}");
            _uploaded.Add(e.Job);
            // Set a timer for 3 minutes, on expiration of that timer, check for available downloads.
            Timer timer = new Timer(OnCheckForDownload, null, 3 * 60 * 1000, Timeout.Infinite);
        }

        private void OnCheckForDownload(object state)
        {
            System.Console.WriteLine("Processing files already uploaded to converter.");
            _converted.Process(_panther);   
        }

        private void OnProcessed(object state, ProcessedSettlementEventArgs eventArgs)
        {
            // Remove from the queue.
            string id = eventArgs.settlement.SettlementId;
            var job = _uploaded.Find(s => s.SettlementId == id);
            if (job != null)
                _uploaded.Remove(job);
            if (!HasUploads() && Finished != null)
                Finished(this, null);
        }
    }
}