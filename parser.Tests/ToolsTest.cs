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
            PrintYearWeek(DateTime.Parse("2019/12/7"));
            PrintYearWeek(DateTime.Parse("2019/12/10"));
            PrintYearWeek(DateTime.Parse("2019/12/14"));
            PrintYearWeek(DateTime.Parse("2019/12/16"));
            PrintYearWeek(DateTime.Parse("2019/12/24"));
            PrintYearWeek(DateTime.Parse("2019/12/31"));
            PrintYearWeek(DateTime.Parse("2020/1/3"));
            PrintYearWeek(DateTime.Parse("2020/1/10"));
            PrintYearWeek(DateTime.Parse("2020/1/16"));
            PrintYearWeek(DateTime.Parse("2020/12/8"));
            PrintYearWeek(DateTime.Parse("2020/12/16"));

            DateTime settlementDate = DateTime.Parse("2019/01/14");
            int year, week;
            Tools.GetWeekNumber(settlementDate, out week, out year);
            Assert.True(year == 2018, $"Year should be 2018, not {year}");
        }

        private void PrintYearWeek(DateTime settlementDate)
        {
            int year, week;
            Tools.GetWeekNumber(settlementDate, out week, out year);
            System.Console.WriteLine($"{settlementDate.ToString("yyyy-MM-dd")}, {year}, {week}");
        }
    }
}
