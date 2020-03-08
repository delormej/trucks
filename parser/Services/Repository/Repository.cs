using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using Azure.Cosmos;
using System.Threading.Tasks;

namespace Trucks
{
    public abstract class Repository
    {
        private const string endpointUrl = "https://trucksdb.documents.azure.com:443/";
        private string authorizationKey = Environment.GetEnvironmentVariable("TRUCKDBKEY");
        protected string databaseId = "trucksdb";

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
                await CreateContainerAsync();
            }
        }

        /// <summary>
        /// Save entity to cosmodb.
        /// </summary>
        public async Task SaveAsync<T>(T item)
        {
            try
            {
                using (CosmosClient cosmosClient = GetCosmosClient())
                {
                    await AddItemsToContainerAsync<T>(cosmosClient, item);
                }
            }
            catch (Exception e)
            {
                System.Console.WriteLine($"Error attempting to save item to CosmosDb\n\t"+ e.Message);
                throw e;
            }
        } 

        /// <summary>
        /// Get all entities of a specific type.
        /// </summary>
        public async Task<List<T>> GetAsync<T>()
        {
            List<T> items = new List<T>();
            using (CosmosClient cosmosClient = GetCosmosClient())
            {
                var sqlQueryText = "SELECT * FROM c";

                Container container = cosmosClient.GetContainer(databaseId, nameof(T));
                QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
                
                await foreach (T item in 
                    container.GetItemQueryIterator<T>(queryDefinition))
                        items.Add(item);
            }
            
            return items;
        }

        /// <summary>
        /// Create the database if it does not exist
        /// </summary>
        protected async Task CreateDatabaseAsync()
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
        protected async Task CreateContainerAsync(string containerId, string partitionKeyPath)
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
        protected async Task AddItemsToContainerAsync<T>(CosmosClient cosmosClient, T value)
        {
            string containerId = typeof(T).Name;

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

        protected CosmosClient GetCosmosClient()
        {
            CosmosClientOptions options = new CosmosClientOptions();
            options.MaxRetryAttemptsOnRateLimitedRequests = 9;
            CosmosClient client = new CosmosClient(endpointUrl, authorizationKey, options);
            return client;
        }

        protected void SetThroughput(CosmosClient cosmosClient, int throughput)
        {
            Database db = cosmosClient.GetDatabase(databaseId);
            var throughputTask = SetContainerThroughputAsync(db, throughput, "SettlementHistory");
            throughputTask.Wait();
            System.Console.WriteLine($"Set throughput to {throughput}.");
        }

        protected async Task SetContainerThroughputAsync(Database db, int throughput, string containerName)
        {
            Container container = db.GetContainer(containerName);
            await container.ReplaceThroughputAsync(throughput);
        }

        protected abstract Task CreateContainerAsync();
    }
}