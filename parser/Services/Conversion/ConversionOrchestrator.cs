using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Trucks
{
    class ConversionOrchestrator
    {
        ExcelConverter converter;
        
        public ConversionOrchestrator(ExcelConverter converter)
        {
            this.converter = converter;
        }

        /// <summary>
        /// Downloads settlements from panther, uploads to converter, and puts on a queue to be checked later.
        /// </summary>
        public async Task StartAsync(SettlementService settlementService, PantherClient panther, int max = 10)
        {
            System.Console.WriteLine("Downloading settlements from panther and uploading to conversion service.");

            List<KeyValuePair<string, SettlementHistory>> downloads = 
                await settlementService.DownloadMissingSettlements(panther, max);
            
            foreach (var download in downloads)
            {
                string filename = download.Key;

                ConversionJob job = new ConversionJob();
                job.Result = await converter.UploadAsync(filename);
                job.Company = download.Value.CompanyId.ToString();
                job.SettlementId = download.Value.SettlementId;
                job.SettlementDate = download.Value.SettlementDate;
             
                QueueUploaded(job);
            }
        }

        public async Task SaveAsync(ConversionJob job, SettlementRepository repository)
        {
            if (job.Result == null)
                throw new ApplicationException("Null ZamzarResult in job object, cannot attempt download.");

            int jobId = job.Result.id;
            ZamzarResult result = await converter.QueryAsync(jobId);
            if (result.status == "failed")
            {
                System.Console.WriteLine($"Failed to convert {job.Result.id}");
                await RemoveJobAsync(job);
            }
            else if (result.status == "successful")
            {
                string convertedFile = job.SettlementId + ".xslx";
                await converter.DownloadAsync(jobId, convertedFile);

                SettlementHistoryParser parser = new SettlementHistoryParser(convertedFile, job.SettlementId);
                SettlementHistory settlement = parser.Parse();
                
                if (settlement != null)
                {
                    settlement.SettlementDate = job.SettlementDate;
                    settlement.SettlementId = job.SettlementId;
                    settlement.CompanyId = int.Parse(job.Company);                    
                    await repository.SaveSettlementHistoryAsync(settlement);
                    await RemoveJobAsync(job);
                }
            }
        }

        /// <summary>
        /// Places the result of the upload on a queue for another process to dequeue when ready.
        /// </summary>
        private void QueueUploaded(ConversionJob job)
        { /* Call DAPR? */ }        

        private async Task RemoveJobAsync(ConversionJob job)
        {
            if (job.Result.target_files != null)
            {
                int fileId = job.Result.target_files[0].id;
                System.Console.WriteLine($"Attempting to remove file id: {fileId.ToString()}");
                await converter.DeleteAsync(fileId);
            }            
        }
    }

    public class ConversionJob
    {
        public ZamzarResult Result { get; set; }
        public string Company { get; set; }
        public string SettlementId { get; set; }
        public DateTime SettlementDate { get; set; }
    }
}