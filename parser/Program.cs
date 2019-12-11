using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Trucks
{
    class Program
    {
        static void Main(string[] args)
        {
            SettlementHistoryParser shParser = new SettlementHistoryParser("sample/converted.xlsx", "CD2222", DateTime.Now);
            SettlementHistory settlement = shParser.Parse();
            System.Console.WriteLine(settlement.ToString());
            return;

            if (args.Length < 1)
            {
                ShowUsage();
                return;
            }
            
            string file = args[0];
            if (!File.Exists(file))
                throw new FileNotFoundException(file);
            
            string csv = File.ReadAllText(file);
            if (csv.Length > 0)
            {
                RevenueDetailParser parser = new RevenueDetailParser();
                List<RevenueDetail> details = parser.LoadFromCsv(csv); 
                CreateSettlementStatements(details);
            }
        }

        private static void CreateSettlementStatements(List<RevenueDetail> details)
        {
            // select by truck
            var truckLoads = 
                from load in details
                group load by load.Truck into g 
                orderby g.Key
                select g;

            foreach (IGrouping<string, RevenueDetail> g in truckLoads)
            {
                Console.WriteLine("Group: " + g.Key);
                foreach (var load in g)
                {
                    Console.WriteLine("\t{0}, {1}", load.Truck, load.Date);
                }
            }
            return;

            using (ExcelWorkbook workbook = new ExcelWorkbook())
            {
                workbook.Open("Settlement.xlsx");
                //string value = workbook.GetCellValue("Week_27", "B1");
                workbook.UpdateCellValue("Week_27", "B1", "Johnny Cheekie");
                workbook.UpdateCellValue("Week_27", "C15", "100.91");
                
                //Console.WriteLine(value);
            }           
        }

        private static void ShowUsage()
        {
            Console.WriteLine("parser <input.csv>");
        }
    }
}
