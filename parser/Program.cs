using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Trucks
{
    class Program
    {
        static void Main(string[] args)
        {
            PayrollHistHtmlParser payrollParser = new PayrollHistHtmlParser();
            payrollParser.Parse("./PayrollHist.html");
            return;

            // if (args.Length < 1)
            // {
            //     ShowUsage();
            //     return;
            // }
            
            // string file = args[0];
            // if (!File.Exists(file))
            //     throw new FileNotFoundException(file);
            string filename = "sample/converted.xlsx";
            int companyId = 33357;
            string settlementId = "CD2224";
            DateTime settlementDate = DateTime.Parse("9/9/2019");

            SettlementHistoryParser shParser = new SettlementHistoryParser(filename, settlementId);
            SettlementHistory settlement = shParser.Parse();
            settlement.SettlementDate = settlementDate;
            settlement.CompanyId = companyId;
            
            try
            {
                Repository repository = new Repository();
                repository.SaveSettlementHistory(settlement).Wait();
                System.Console.WriteLine("Wrote to table");
            }
            catch (Exception e)
            {
                System.Console.WriteLine("ERROR: " + e);
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
            
        }
    }
}
