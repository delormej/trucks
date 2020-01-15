using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using Azure.Cosmos;
using System.Threading.Tasks;

namespace Trucks
{
    public class SettlementRepository : Repository
    {
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

        public async Task<List<SettlementHistory>> GetSettlementsByWeekAsync(int year, int[] weeks)
        {
            string weekNumbers = string.Join(',', weeks);

            using (CosmosClient cosmosClient = GetCosmosClient())
            {
                try 
                {
                    List<SettlementHistory> items = new List<SettlementHistory>();    
                    string sqlQueryText = $"SELECT * FROM SettlementHistory c";
                    sqlQueryText += $" WHERE c.Year = {year} AND c.WeekNumber IN ({weekNumbers})";

                    Container container = cosmosClient.GetContainer(databaseId, "SettlementHistory");
                    QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
                    
                    await foreach (SettlementHistory item in 
                        container.GetItemQueryIterator<SettlementHistory>(queryDefinition))
                            items.Add(item);

                    return items;
                }
                catch (Exception e)
                {
                    System.Console.WriteLine($"Error occurred while retrieving SettlementHistory for weeks: {weekNumbers}:\n\t" +
                        e.Message);
                    return null;
                }
            }
        }

        public void SaveSettlements(List<SettlementHistory> settlements)
        {
            using (CosmosClient cosmosClient = GetCosmosClient())
            {
                // SetThroughput(cosmosClient, 10000);
                foreach (SettlementHistory settlement in settlements)
                {
                    try
                    {
                        // Run 1 settlement at a time.
                        Task task = Task.Run(() => AddItemsToContainerAsync<SettlementHistory>(cosmosClient, settlement));
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
                // SetThroughput(cosmosClient, 400);
            }
        }

        public async Task SaveSettlementHistoryAsync(SettlementHistory settlement)
        {
            try
            {
                using (CosmosClient cosmosClient = GetCosmosClient())
                {
                    await AddItemsToContainerAsync<SettlementHistory>(cosmosClient, settlement);
                }
            }
            catch (Exception e)
            {
                System.Console.WriteLine($"Error attempting to write settlement {settlement.SettlementId} to CosmosDb\n\t"+ e.Message);
                throw e;
            }
        } 
        public async Task ConsolidateSettlementsAsync()
        {
            using (CosmosClient cosmosClient = GetCosmosClient())
            {
                List<SettlementHistory> settlements = 
                    await GetSettlementItemsAsync<SettlementHistory>(cosmosClient);
                
                foreach (var settlement in settlements)
                {
                    // settlement.Credits = 
                    //     await GetSettlementItemsAsync<Credit>(cosmosClient, settlement.SettlementId);
                    // settlement.Deductions = 
                    //     await GetSettlementItemsAsync<Deduction>(cosmosClient, settlement.SettlementId);
                    
                    if (settlement.Credits?.Count > 0 && settlement.Deductions?.Count > 0)
                    {
                        //await AddItemsToContainerAsync<SettlementHistory>(cosmosClient, settlement, "SettlementHistory");
                        
                        //System.Console.WriteLine($"Updated settlement {settlement.SettlementId} with {settlement.Credits?.Count()} credits and {settlement.Deductions?.Count()} deducations.");
                    }
                    else
                    {
                        // Remove settlements that do not have credits or deductions.
                        System.Console.WriteLine($"{settlement.SettlementId} has no credits/deductions.");
                        await DeleteSettlementAsync(cosmosClient, settlement);
                    }
                }
            }
        }

        private async Task<List<T>> GetSettlementItemsAsync<T>(CosmosClient cosmosClient, string settlementId = null) where T : SettlementItem
        {
            string itemName = typeof(T).Name;

            try 
            {
                List<T> items = new List<T>();    
                string sqlQueryText = $"SELECT * FROM {itemName} c";
                if (settlementId != null) 
                    sqlQueryText += $" WHERE c.SettlementId = '{settlementId}'";

                Container container = cosmosClient.GetContainer(databaseId, itemName);
                QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
                
                await foreach (T item in 
                    container.GetItemQueryIterator<T>(queryDefinition))
                        items.Add(item);

                return items;
            }
            catch (Exception e)
            {
                System.Console.WriteLine($"Error occurred while retrieving {itemName} with {settlementId}:\n\t" +
                    e.Message);
                return null;
            }
        }

        public async Task<SettlementHistory> GetSettlementAsync(string settlementId, string companyId)
        {
            using (var cosmosClient = GetCosmosClient())
            {
                Container container = cosmosClient.GetContainer(databaseId, 
                    typeof(SettlementHistory).Name);
                
                SettlementHistory settlement = 
                    await container.ReadItemAsync<SettlementHistory>(settlementId, 
                        new PartitionKey(double.Parse(companyId)));
                
                return settlement;
            }
        }

        private async Task DeleteSettlementAsync(CosmosClient cosmosClient, SettlementHistory settlement)
        {
            Container container = cosmosClient.GetContainer(databaseId, typeof(SettlementHistory).Name);
            PartitionKey partitionKey = new PartitionKey(settlement.CompanyId);
            await container.DeleteItemAsync<SettlementHistory>(settlement.SettlementId, partitionKey);
            System.Console.WriteLine($"Deleted: {settlement.SettlementId}");
        }
    }
}