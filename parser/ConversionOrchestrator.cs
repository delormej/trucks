using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Trucks
{
    class ConversionOrchestrator
    {
        Queue<SettlementHistory> queue;
        ExcelConverter converter;
        List<Timer> jobs;

        Dictionary<int, SettlementHistory> pendingDownload;

        Repository repository;                

        // 1. Create orchestrator with list of downloaded files.
        // 2. Upload xls to converter
        //      [Restrict 20 uploads at a time]
        // 3. Track upload job periodically (every 5 min?)
        //      [Resume uploads when the active count of conversion is <20]
        // 4. Download converted xlsx
        // 5. Parse xlsx 
        // 6. Persist data to database

        public ConversionOrchestrator(List<SettlementHistory> settlements, string apiKey)
        {
            queue = new Queue<SettlementHistory>(settlements);
            converter = new ExcelConverter(apiKey);
            jobs = new List<Timer>();

            pendingDownload = new Dictionary<int, SettlementHistory>();
            repository = new Repository();
        }

        public async Task StartAsync()
        {
            const int maxUploadCount = 20;
            await UploadBatchAsync(maxUploadCount);

            // Wait until the queue is empty.
            await Task.Run( () => {
                while (pendingDownload.Count > 0);
            });
        }

        private async Task UploadBatchAsync(int max)
        {
            foreach (SettlementHistory settlement in queue)
            {
                System.Console.WriteLine($"Uploading settlement: {settlement.SettlementId}");
                var result = await converter.UploadAsync(GetLocalFilename(settlement));
                
                ScheduleJob(result.id, settlement);
                pendingDownload.Add(result.id, settlement);
            }
        }

        private void ScheduleJob(int jobId, SettlementHistory settlement)
        {
            JobStatus job = new JobStatus(jobId, settlement);
            const int checkDelayMs = 60 * 1000;

            jobs.Add(
                new Timer(DownloadIfReady, job, checkDelayMs, Timeout.Infinite)
            );
        }

        /// <summary>
        /// Returns the local file name of a settlement history item.
        /// </summary>
        private string GetLocalFilename(SettlementHistory item)
        {
            return Path.Join(item.CompanyId.ToString(), item.SettlementId + ".xls");
        }

        private void DownloadIfReady(Object stateInfo)
        {
            JobStatus job = (JobStatus)stateInfo;
            var task = converter.QueryAsync(job.JobId);
            task.Wait();
            ZamzarResult result = task.Result;

            if (result.status == "failed")
            {
                System.Console.WriteLine($"Failed to convert {job.Item.SettlementId}");
                pendingDownload.Remove(job.JobId);
            }
            else if (result.status == "successful")
            {
                // queue download, then save to db
                string convertedFile = job.Item.SettlementId + ".xslx";
                var download = converter.DownloadAsync(job.JobId.ToString(), convertedFile);
                download.Wait();
                pendingDownload.Remove(job.JobId);
                SettlementHistoryParser parser = new SettlementHistoryParser(convertedFile, job.Item.SettlementId);
                SettlementHistory settlement = parser.Parse();
                Task.Run( async () => {
                    await repository.SaveSettlementHistoryAsync(settlement);
                });
            }
            else
            {
                // check again at some interval
                ScheduleJob(job.JobId, job.Item);
            }
        }
    }

    public class JobStatus
    {
        public int JobId { get; set; }
        public SettlementHistory Item { get; set; }

        public JobStatus(int jobId, SettlementHistory item)
        {
            this.JobId = jobId;
            this.Item = item;
        }
    }
}