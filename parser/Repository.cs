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

        public Repository()
        {
            if (authorizationKey == null)
                throw new ApplicationException("Must set TRUCKDBKEY environment variable with authorization key for CosmosDb.");
        }

        /// <summary>
        /// Ensures the database structure is in place.
        /// </summary>
        public async Task EnsureDatabaseAsync()
        {
            using (CosmosClient cosmosClient = GetCosmosClient())
            {
                await CreateDatabaseAsync();
                await CreateContainerAsync("SettlementHistory", "/CompanyId");
                await CreateContainerAsync("Credit", "/SettlementId");
                await CreateContainerAsync("Deduction", "/SettlementId");            
            }
        }

        public async Task<List<SettlementHistory>> GetSettlementsAsync()
        {
            List<SettlementHistory> settlements = new List<SettlementHistory>();
            using (CosmosClient cosmosClient = GetCosmosClient())
            {
                var sqlQueryText = "SELECT * FROM SettlementHistory h";

                Container container = cosmosClient.GetContainer(databaseId, "SettlementHistory");
                QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
                
                await foreach (SettlementHistory settlement in 
                    container.GetItemQueryIterator<SettlementHistory>(queryDefinition))
                        settlements.Add(settlement);
            }
            return settlements;
        }

        public async Task SaveSettlementHistoryAsync(SettlementHistory settlement)
        {
            try
            {
                await InsertSettlementAsync(settlement);
            }
            catch (Exception e)
            {
                System.Console.WriteLine($"Error attempting to write settlement {settlement.SettlementId} to CosmosDb\n\t"+ e.Message);
                throw e;
            }
        } 


        /// <summary>
        /// Create the database if it does not exist
        /// </summary>
        private async Task CreateDatabaseAsync()
        {
            // Create a new database
            using (CosmosClient cosmosClient = GetCosmosClient())
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
            using (CosmosClient cosmosClient = GetCosmosClient())
            {
                Container container = await cosmosClient.GetDatabase(databaseId)
                    .CreateContainerIfNotExistsAsync(containerId, partitionKeyPath);
                Console.WriteLine("Created Container: {0}\n", container.Id);
            }
        }        

        /// <summary>
        /// Add Family items to the container
        /// </summary>
        private async Task AddItemsToContainerAsync<T>(CosmosClient cosmosClient, T value, string containerId)
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

        private async Task InsertSettlementAsync(SettlementHistory settlement)
        {
            //
            // The challenge with this is that it's not a transaction and compensating
            // logic will need to be written in the event of a FAILURE.
            // Alternatively, we could write a JavaScript stored procedure which takes
            // the fully hydrated deep Settlement object and does this on the server side
            // encapsulating it in a transaction.
            //

            using (CosmosClient cosmosClient = GetCosmosClient())
            {
                List<Task> inserts = new List<Task>();
                foreach (Credit credit in settlement.Credits)
                {
                    inserts.Add(AddItemsToContainerAsync<Credit>(cosmosClient, credit, "Credit"));
                }
                foreach (Deduction deduction in settlement.Deductions)
                {
                    inserts.Add(AddItemsToContainerAsync<Deduction>(cosmosClient, deduction, "Deduction"));
                }
                await Task.WhenAll(inserts);

                // Remove these for shallow insert.
                settlement.Credits = null;
                settlement.Deductions = null;

                await AddItemsToContainerAsync<SettlementHistory>(cosmosClient, settlement, "SettlementHistory");            
            }
        }

        private CosmosClient GetCosmosClient()
        {
            return new CosmosClient(endpointUrl, authorizationKey);
        }        
    }
}