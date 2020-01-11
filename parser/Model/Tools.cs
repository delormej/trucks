using System;
using System.Globalization;

namespace Trucks
{
    public class Tools
    {
        public static void GetWeekNumber(DateTime settlementDate, out int week, out int year)
        {
            //source: https://stackoverflow.com/questions/11154673/get-the-correct-week-number-of-a-given-date

            // Seriously cheat.  If its Monday, Tuesday or Wednesday, then it'll 
            // be the same week# as whatever Thursday, Friday or Saturday are,
            // and we always get those right
            DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(settlementDate);

            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                settlementDate = settlementDate.AddDays(3);
            }

            // Return the week of our adjusted day
            int actualWeek = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                settlementDate, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

            if (actualWeek > 51)
            {
                week = actualWeek + 1 - 52;
                year = settlementDate.Year + 1;
            }
            else
            {
                week = actualWeek + 1;
                year = settlementDate.Year;
            }
        }
    }
}