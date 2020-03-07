using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Azure.Identity;
using Azure.Storage.Blobs;
using Trucks;

namespace settlement.webapi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SettlementController : ControllerBase
    {
        private readonly ILogger<SettlementController> _logger;

        public SettlementController(ILogger<SettlementController> logger)
        {
            _logger = logger; 
        }

        [HttpGet]
        [Route("hello")]
        public IActionResult Hello()
        {
            return Ok("Hello");
        }

        /// <summary>
        /// Returns an array of companyId, settlementId for those settlments processed.
        /// </summary>
        [HttpPost]
        [Route("missing")]
        public async Task<IActionResult> GetMissingSettlements(string companyId, string pantherPassword)
        {
            SettlementService settlementService = new SettlementService();
            PantherClient panther = new PantherClient(companyId, pantherPassword);
            List<SettlementHistory> settlements = 
                await settlementService.GetMissingSettlementsAsync(panther);
            
            List<SavedSettlement> savedSettlements = new List<SavedSettlement>();
            
            foreach (var settlement in settlements)
            {
                Stream stream = await panther.GetSettlementReportStreamAsync(settlement.SettlementId);
                SavedSettlement saved = await SaveBlob(settlement.CompanyId, settlement.SettlementId, stream);
                savedSettlements.Add(saved);
            }

            return Ok(savedSettlements);
        }            

        private async Task<SavedSettlement> SaveBlob(int companyId, string settlementId, Stream stream)
        {
            const string accountName = "truckstorage";
            const string containerName = "pantherdownloads";
            string containerEndpoint = string.Format("https://{0}.blob.core.windows.net/{1}",
                accountName, containerName);   
            string blobName = string.Format("{0}/{1}", companyId.ToString(), settlementId);

            BlobContainerClient containerClient = new BlobContainerClient(new Uri(containerEndpoint),
                new DefaultAzureCredential());
            
            SavedSettlement saved = null;

            try
            {
                await containerClient.CreateIfNotExistsAsync();
                await containerClient.UploadBlobAsync(blobName, stream);
                saved = new SavedSettlement() { 
                    CompanyId = companyId,
                    SettlementId = settlementId,
                    BlobUri = containerEndpoint + "/" + blobName
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Unable to save blob for {settlementId}.");
            }

            return saved;
        }

        private class SavedSettlement
        {
            public int CompanyId;
            public string SettlementId;
            public string BlobUri;
        }
    }
}
