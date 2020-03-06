using System;
using Xunit;
using Trucks;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace parser.Tests
{
    public class RepositoryTest
    {
        [Fact]
        public void TestFuelChargeRepository()
        {
            FuelChargeRepository fuelRepo = new FuelChargeRepository(2020, new int[] { 1 });
            var charges = fuelRepo.GetFuelCharges(2020, 1, 15172);
            Assert.True(charges == 581.31, "Charges do not equal proper amount.");
        }

        [Fact]
        private static void TestSettlementRepository()
        {
            SettlementRepository repo = new SettlementRepository();
            List<SettlementHistory> settlements = null;
            Task.Run( async () => {
                settlements = await repo.GetSettlementsAsync();
            }).Wait();
            Assert.True(settlements.Count > 100, "Did find settlements.");
        }

        [Fact]
        private static void TestTruckRepository()
        {
            Truck truck = new Truck();
            truck.TruckId = "14288";
            Driver driver = new Driver() {
                Name = "John Doe",
                State = "TN",
                SocialSecurityNumber = "500-10-5000"
            };
            truck.InServiceHistory = new List<TruckInService>();
            truck.InServiceHistory.Add(new TruckInService() {
                Driver = driver,
                InServiceDate = new DateTime(2020, 1, 1)
            });

            Task.Run( async () => {
                TruckRepository repo = new TruckRepository();
                await repo.EnsureDatabaseAsync();
                await repo.SaveAsync<Truck>(truck);
            }).Wait();
        }
    }
}
            
