using System;

namespace Trucks
{
    public class CreateSettlementOptions
    {
        public int Year;
        public string[] Companies;
        public int[] Weeks;
        public int TruckId;

        public static CreateSettlementOptions LastWeek()
        {
            int week, year;
            Tools.GetWeekNumber(DateTime.Now, out week, out year);

            return new CreateSettlementOptions() {
                Year = year,
                Weeks = new int[] { week }
            };
        }

        public override string ToString()
        {
            // TODO: stringify this object in some way.
            return "options...";
        }
    }
}