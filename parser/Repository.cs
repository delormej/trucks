using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Cosmos;

namespace Trucks
{
    class Repository
    {
        // The Azure Cosmos DB endpoint for running this sample.
        private static readonly string EndpointUri = ConfigurationManager.AppSettings["EndPointUri"];

        // The primary key for the Azure Cosmos account.
        private static readonly string PrimaryKey = ConfigurationManager.AppSettings["PrimaryKey"];

        // The Cosmos client instance
        private CosmosClient cosmosClient;

        // The database we will create
        private Database database;

        // The container we will create.
        private Container container;

        // The name of the database and container we will create
        private string databaseId = "trucksdb";
        private string containerId = "SettlementHistory";        
    }
}