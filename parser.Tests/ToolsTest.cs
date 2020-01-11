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
            PrintYearWeek(DateTime.Parse("2019/12/6"));
            
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
    }
}
