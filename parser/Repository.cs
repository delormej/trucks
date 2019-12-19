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
        private string authorizationKey = Environment.GetEnvironmentVariable("TRUCKDBKEY");
        private const string databaseId = "trucks";

        private CosmosClient cosmosClient;

        public Repository()
        {
            if (authorizationKey == null)
                throw new ApplicationException("Must set TRUCKDBKEY environment variable with authorization key for CosmosDb.");
        }

        /// <summary>
        /// Create the database if it does not exist
        /// </summary>
        private async Task CreateDatabaseAsync()
        {
            // Create a new database
            using (cosmosClient = new CosmosClient(endpointUrl, authorizationKey))
            {
                Database database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
                Console.WriteLine("Created Database: {0}\n", database.Id);
            }
        }

        /// <summary>
        /// Create the container if it does not exist. 
        /// Specify "/LastName" as the partition key since we're storing family information, to ensure good distribution of requests and storage.
        /// </summary>
        /// <returns></returns>
        private async Task CreateContainerAsync(string containerId, string partitionKeyPath)
        {
            // Create a new container
            using (cosmosClient = new CosmosClient(endpointUrl, authorizationKey))
            {
                Container container = await cosmosClient.GetDatabase(databaseId)
                    .CreateContainerIfNotExistsAsync(containerId, partitionKeyPath);
                Console.WriteLine("Created Container: {0}\n", container.Id);
            }
        }        

        /// <summary>
        /// Add Family items to the container
        /// </summary>
        private async Task AddItemsToContainerAsync<T>(T value, string containerId)
        {
            try
            {
                Container container = cosmosClient.GetContainer(databaseId, containerId);
                ItemResponse<T> response = await container.UpsertItemAsync<T>(value);
            }
            catch (Exception e)
            {
                string valueText = System.Text.Json.JsonSerializer.Serialize(value, typeof(T));
                System.Console.WriteLine($"Unable to save {valueText} in {containerId}, error:\n" + e.Message + "\n" + e.StackTrace);
            }
        }    

        public async Task SaveSettlementHistoryAsync(SettlementHistory settlement)
        {
            try
            {
                await EnsureDatabaseAsync();
                await InsertSettlementAsync(settlement);
            }
            catch (Exception e)
            {
                System.Console.WriteLine($"Error attempting to write settlement {settlement.SettlementId} to CosmosDb\n\t"+ e.Message);
                throw e;
            }
        }    

        /// <summary>
        /// Ensures the database structure is in place.
        /// </summary>
        private async Task EnsureDatabaseAsync()
        {
            using (cosmosClient = new CosmosClient(endpointUrl, authorizationKey))
            {
                await CreateDatabaseAsync();
                await CreateContainerAsync("SettlementHistory", "/CompanyId");
                await CreateContainerAsync("Credit", "/SettlementId");
                await CreateContainerAsync("Deduction", "/SettlementId");            
            }
        }

        private async Task InsertSettlementAsync(SettlementHistory settlement)
        {
            //
            // The challenge with this is that it's not a transaction and compensating
            // logic will need to be written in the event of a FAILURE.
            // Alternatively, we could write a JavaScript stored procedure which takes
            // the fully hydrated deep Settlement object and does this on the server side
            // encapsulating it in a transaction.
            //

            using (cosmosClient = new CosmosClient(endpointUrl, authorizationKey))
            {
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
}