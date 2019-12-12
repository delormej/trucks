using System;

namespace Trucks
{
    public class Deduction : SettlementItem
    {
        public Deduction() {}
        public Deduction(string settlementId) : base(settlementId){}

        // Required for CosmosDb
        public string id { get { return Guid.NewGuid().ToString(); } set {} }

        [SheetColumn("DATE")]
        public string Date { get; set; }

        [SheetColumn("CARR/DRIVER")]
        public string Driver { get; set; }

        [SheetColumn("CARR INV/TRK")]
        public int TruckId { get; set; }

        [SheetColumn("DESCRIPTION")]
        public string Description { get; set; }        

        [SheetColumn("DEDUCTION AMOUNT")]
        public double Amount { get; set; }    

        [SheetColumn("TOTAL DEDUCTIONS")]
        public double TotalDeductions { get; set; }
    }
}