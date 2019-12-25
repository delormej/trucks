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

        public void SaveSettlements(List<SettlementHistory> settlements, string company)
        {
            using (CosmosClient cosmosClient = GetCosmosClient())
            {
                SetThroughput(cosmosClient, 10000);
                foreach (SettlementHistory settlement in settlements)
                {
                    try
                    {
                        // Run 1 settlement at a time.
                        Task task = Task.Run(() => SaveSettlementHistoryAsync(cosmosClient, settlement));
                        task.Wait();
                        if (task.Exception != null)
                            throw task.Exception;
                        System.Console.WriteLine($"Saved settlement id: {settlement.SettlementId}.");
                    }
                    catch (Exception e)
                    {
                        System.Console.WriteLine(
                            $"Error atempting to save settlement: {settlement.id} to database.\n\t{e.Message}");
                    }
                }
                SetThroughput(cosmosClient, 400);
            }
        }

        public async Task SaveSettlementHistoryAsync(SettlementHistory settlement)
        {
            try
            {
                using (CosmosClient cosmosClient = GetCosmosClient())
                {
                    await SaveSettlementHistoryAsync(cosmosClient, settlement);
                }
            }
            catch (Exception e)
            {
                System.Console.WriteLine($"Error attempting to write settlement {settlement.SettlementId} to CosmosDb\n\t"+ e.Message);
                throw e;
            }
        } 

        private async Task SaveSettlementHistoryAsync(CosmosClient cosmosClient, SettlementHistory settlement)
        {
            List<Task> inserts = new List<Task>();
            if (settlement.Credits != null)
            {
                foreach (Credit credit in settlement.Credits)
                    inserts.Add(AddItemsToContainerAsync<Credit>(cosmosClient, credit, "Credit"));
            }
            if (settlement.Deductions != null)
            {
                foreach (Deduction deduction in settlement.Deductions)
                    inserts.Add(AddItemsToContainerAsync<Deduction>(cosmosClient, deduction, "Deduction"));
            }
            if (inserts.Count > 0)
            {
                await Task.WhenAll(inserts);
                // Remove these for shallow insert.
                settlement.Credits = null;
                settlement.Deductions = null;
            }

            await AddItemsToContainerAsync<SettlementHistory>(cosmosClient, settlement, "SettlementHistory");            
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

        private CosmosClient GetCosmosClient()
        {
            return new CosmosClient(endpointUrl, authorizationKey);
        }

        private void SetThroughput(CosmosClient cosmosClient, int throughput)
        {
            Database db = cosmosClient.GetDatabase(databaseId);
            var creditTask = SetContainerThroughputAsync(db, throughput, "Credit");
            var deductionTask = SetContainerThroughputAsync(db, throughput, "Deduction");
            Task.WaitAll(creditTask, deductionTask);
            System.Console.WriteLine($"Set throughput to {throughput}.");
        }

        private async Task SetContainerThroughputAsync(Database db, int throughput, string containerName)
        {
            Container container = db.GetContainer(containerName);
            await container.ReplaceThroughputAsync(throughput);
        }
    }
}