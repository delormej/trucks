using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using Azure.Cosmos;
using System.Threading.Tasks;

namespace Trucks
{
    class Repository
    {
        private const string endpointUrl = "https://trucksdb.documents.azure.com:443/";
        private string authorizationKey = Environment.GetEnvironmentVariable("TRUCKDB_KEY");
        private const string databaseId = "trucks";

        private CosmosClient cosmosClient;

        /// <summary>
        /// Create the database if it does not exist
        /// </summary>
        private async Task CreateDatabaseAsync()
        {
            // Create a new database
            Database database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
            Console.WriteLine("Created Database: {0}\n", database.Id);
        }

        /// <summary>
        /// Create the container if it does not exist. 
        /// Specify "/LastName" as the partition key since we're storing family information, to ensure good distribution of requests and storage.
        /// </summary>
        /// <returns></returns>
        private async Task CreateContainerAsync(string containerId, string partitionKeyPath)
        {
            // Create a new container
            Container container = await cosmosClient.GetDatabase(databaseId)
                .CreateContainerIfNotExistsAsync(containerId, partitionKeyPath);
            Console.WriteLine("Created Container: {0}\n", container.Id);
        }        

        /// <summary>
        /// Add Family items to the container
        /// </summary>
        private async Task AddItemsToContainerAsync<T>(T value, string containerId)
        {
            Container container = cosmosClient.GetContainer(databaseId, containerId);
            ItemResponse<T> response = 
                await container.CreateItemAsync<T>(value);
        }    

        public async Task SaveSettlementHistory(SettlementHistory settlement)
        {
            cosmosClient = new CosmosClient(endpointUrl, authorizationKey);
            await CreateDatabaseAsync();
            await CreateContainerAsync("SettlementHistory", "/CompanyId");
            await CreateContainerAsync("Credit", "/SettlementId");
            await CreateContainerAsync("Deduction", "/SettlementId");

            List<Task> inserts = new List<Task>();
            foreach (Credit credit in settlement.Credits)
            {
                inserts.Add(AddItemsToContainerAsync<Credit>(credit, "Credit"));
            }
            foreach (Deduction deduction in settlement.Deductions)
            {
                inserts.Add(AddItemsToContainerAsync<Deduction>(deduction, "Deduction"));
            }

            await Task.WhenAll(inserts);
            // Remove these for shallow insert.
            settlement.Credits = null;
            settlement.Deductions = null;
            await AddItemsToContainerAsync<SettlementHistory>(settlement, "SettlementHistory");
        }    
    }
}