using System;
using Xunit;
using Trucks;

namespace parser.Tests
{
    public class ToolsTest
    {
        [Fact]
        public void TestGetPayrollWeekYear()
        {
            PrintYearWeek(DateTime.Parse("2019/12/17"));
            
            PrintYearWeek(DateTime.Parse("2019/12/27"), "CD630340 = week 1"); 
            PrintYearWeek(DateTime.Parse("2019/12/13"), "CD629602 = week 51");
            PrintYearWeek(DateTime.Parse("2019/12/20"), "CD629969 = week 52");
            PrintYearWeek(DateTime.Parse("2020/1/3"), "CD630651 = week 2");

            DateTime settlementDate = DateTime.Parse("2019/01/14");
            int year, week;
            Tools.GetWeekNumber(settlementDate, out week, out year);
            Assert.True(year == 2018, $"Year should be 2018, not {year}");
        }

        private void PrintYearWeek(DateTime settlementDate, string settlement = "")
        {
            int year, week;
            Tools.GetWeekNumber(settlementDate, out week, out year);
            System.Console.WriteLine($"{settlement}\t{settlementDate.ToString("yyyy-MM-dd")}, {year}, {week}");
        }

        [Fact]
        public void TestFuelChargeId()
        {
            FuelCharge charge = new FuelCharge();
            charge.TransactionDate = "1/20/2020";
            charge.TransactionTime = "1:48:00";
            charge.NetCost = 24.93;
            System.Console.WriteLine($"id: {charge.id}");

            FuelCharge charge2 = new FuelCharge();
            charge2.TransactionDate = "1/20/2020";
            charge2.TransactionTime = "1:48:01";
            charge2.NetCost = 24.93;
            System.Console.WriteLine($"id: {charge2.id}");

            Assert.True(charge.id != charge2.id);

        }
    }
}
