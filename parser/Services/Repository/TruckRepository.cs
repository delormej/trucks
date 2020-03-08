using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using Azure.Cosmos;
using System.Threading.Tasks;

namespace Trucks
{
    public class TruckRepository : Repository
    {
        protected override async Task CreateContainerAsync()
        {
            await CreateContainerAsync("Truck", "/TruckId");
        }        
    }
}