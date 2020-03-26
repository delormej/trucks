using System;
using System.Collections.Generic;
using System.Linq;
using jasondel.Tools;

namespace Trucks
{
    public class SettlementWorkbookFactory
    {
        public static string Create(IEnumerable<DriverSettlement> driverSettlements)
        {
            SettlementWorkbook workbook = NewWorkbook(driverSettlements);
            string outputFile = workbook.Create();
            
            try
            {
                foreach (var settlement in driverSettlements)
                {                   
                    workbook.AddSheet(settlement.WeekId, settlement.DriverSettlementDate);
                    workbook.AddSettlementId(settlement.SettlementId);

                    //
                    // TODO: currently this is derived *IF* we find fuel charges, however
                    // this logic is actually based on whether the DRIVER is setup for 
                    // comchek or not.  Need to fix this.
                    //
                    #warning FIX THIS: Need to get Comchek flag from Driver.
                    bool ignoreComchek = false;

                    if (settlement.FuelCharges > 0)
                    {
                        workbook.AddFuelCharge(settlement.FuelCharges);
                        ignoreComchek = true;
                    }

                    workbook.AddCredits(settlement.Credits, ignoreComchek);                    
                    
                    if (settlement.OccupationalInsurance > 0)
                        workbook.AddOccupationalInsurance(settlement.OccupationalInsurance);
                        
                    workbook.Save();

                    Logger.Log($"Created {outputFile} with {settlement.Credits.Count()} credit(s), ${settlement.FuelCharges.ToString("0.00")} in fuel from {settlement.id}:{settlement.DriverSettlementDate.ToString("yyyy-MM-dd")} for company {settlement.CompanyId}.");
                }
            }
            catch (Exception e)
            {
                Logger.Log($"Error generating workbook {outputFile ?? "null"}\n\t{e.Message}");
            }
            finally
            {
                if (workbook != null)
                    workbook.Dispose();
            }

            return outputFile;
        }

        /// <summary>
        /// Helper method to initalize a new SettlementWorkbook object from the first 
        /// DriverSettlement.  
        /// </summary>
        /// <note>
        /// The values used to initalize are the same accross all DriverSettlement instances.
        /// </note>
        private static SettlementWorkbook NewWorkbook(IEnumerable<DriverSettlement> settlements)
        {
            DriverSettlement settlement = settlements.First();
            if (settlement == null)
                throw new ApplicationException("No DriverSettlements passed to SettlementWorkbookFactory");
            
            SettlementWorkbook workbook = new SettlementWorkbook(
                settlement.Year, settlement.TruckId, settlement.Driver);

            return workbook;
        }
    }
}