using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace Trucks
{
    public class RevenueDetail
    {
        public string Truck {get; set;}
        public string Load {get;set;}
        public DateTime Date {get;set;}
        public double Total {get;set;}
        public double Miles {get;set;}
        public double Accesorials {get;set;}
        public double Detention {get;set;}
        public double DeadHead {get;set;}
        public double Layover {get;set;}
        public double HandLoad {get;set;}
        public double Tolls {get;set;}
        public double Bonus {get;set;}
        public double StopOff {get;set;}
        public double EmptyMove {get;set;}
        public double Other {get;set;}
        public double Linehaul {get;set;}
        public double FuelSurcharge {get;set;}
    }

    public class WeeklySummary
    {
        public string Truck {get; set;}
        public int Week { get; set; }
        public double NetRevenue { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class RevenueDetailParser
    {
        // Source: https://github.com/gopro/gpmf-parser
        enum Column {
            Load = 0,
            Date = 1,
            Total = 2,
            Miles = 3,
            Accesorials = 4,
            Canada = 5,
            Detention = 6,
            DeadHead = 7,
            Layover = 8,
            HandLoad = 9,
            Tolls = 10,
            Bonus = 11,
            StopOff = 12,
            EmptyMove = 13,
            Other = 14,
            Linehaul = 15,
            FuelSurcharge = 16
        };

        public List<RevenueDetail> LoadFromCsv(string csv)
        {
            Console.WriteLine("Truck, Week, Date, NetRevenue");
            List<RevenueDetail> truckRevenue = new List<RevenueDetail>();

            using (var sr = new StringReader(csv))
            {
                sr.ReadLine(); // advance past 1st line.
                string line = "";
                string truckId = "";
                while ((line = sr.ReadLine()) != null)
                {
                    if (line == "") 
                    {
                        truckId = ReadTruckId(sr);
                    }
                    else
                    {
                        string[] row = line.Split(',');                    
                        RevenueDetail detail = ParseRow(row);
                        truckRevenue.Add(detail);
                        Console.WriteLine("{0}, {1}, {2:MM/dd/yyyy}, {3}", truckId, 
                            GetWeek(detail.Date), detail.Date, GetRevenue(detail));                        
                    }
                }
            }

            //List<WeeklySummary> weekly = RevenueByWeek(truckRevenue);
            //foreach (WeeklySummary week in weekly)
            //    Console.WriteLine("{0}, {1}", week.Week, week.NetRevenue);

            return truckRevenue;
        }

        private RevenueDetail ParseRow(string[] row)
        {
            RevenueDetail detail = new RevenueDetail();
            string date = GetColumn(row, Column.Date);
            
            detail.Date = DateTime.Parse(date);
            detail.Linehaul = GetColumnAsDouble(row, Column.Linehaul);
            // detail.Layover = GetColumnAsDouble(row, Column.Layover);
            // detail.Other = GetColumnAsDouble(row, Column.Other);
            // detail.StopOff = GetColumnAsDouble(row, Column.StopOff);
            // detail.Canada = GetColumnAsDouble(row, Column.Canada);
            detail.Accesorials = GetColumnAsDouble(row, Column.Accesorials);

            return detail;
        }

        private string ReadTruckId(StringReader sr)
        {
            // Revenue Report for Truck: 14334
            const string signature = @"""Revenue Report for Truck:";
            string truckId = null;
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (line.StartsWith(signature)) 
                {
                    int start = line.LastIndexOf(':');
                    if (start > 0)
                    {
                        truckId = line.Substring(start + 2, line.Length - start - 4);
                        if (truckId.Length > 0)
                            break;

                    }
                }
            }

            if (!ValidateHeader(sr.ReadLine()))
            {
                Console.WriteLine("Header does not match expected format.");
                return null;
            }            

            return truckId;
        }

        private bool ValidateHeader(string header)
        {
            const string headerFormat = @"""Pro No"",""Fiscal Date"",""Total $"",""Miles"",""Accessorial $"",""Canada $"",""Detention"",""Dead Head $"",""Layover $"",""Hand Load & Unload $"",""Tolls $"",""Bonus $"",""Stop Off $"",""Empty Move $"",""Other $"",""Linehaul $"",""FSC $"",";
            return (header == headerFormat);
        }

        private string GetColumn(string[] row, Column column)
        {
            if (row.Length < (int)column)
                return "";
            string value = row[(int)column];
            return value.Replace("\"", "");
        }

        private double GetColumnAsDouble(string[] row, Column column)
        {
            string value = GetColumn(row, column);
            return double.Parse(value);
        }

        private List<WeeklySummary> RevenueByWeek(List<RevenueDetail> details)
        {
            //details.Where()
            //var result = from d in details
            //             group GetWeek(d.Date)
            //             select (d.Date, d.Linehaul);


            //             GroupBy(d => GetWeek(d.Date)).Select(d => })

            List<WeeklySummary> summaries = new List<WeeklySummary>();
            WeeklySummary summary = new WeeklySummary() { Week = 1 };
            foreach (var detail in details)
            {
                int week = GetWeek(detail.Date);
                if (week != summary.Week)
                {
                    summaries.Add(summary);
                    summary = new WeeklySummary();
                    summary.Week = week;
                    summary.Truck = detail.Truck;
                }

                summary.NetRevenue += (detail.Linehaul * 0.50);
                summary.NetRevenue += (detail.Accesorials * 0.20);
            }

            return summaries;
        }

        private double GetRevenue(RevenueDetail detail)
        {
            double revenue = 0;
            revenue += (detail.Linehaul * 0.50);
            revenue += (detail.Accesorials * 0.20);            
            return revenue;
        }

        private int GetWeek(DateTime date)
        {
            int week = date.DayOfYear / 7;
            return week + 1;
        }
    }
}
