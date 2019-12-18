using System;

namespace Trucks
{
    public class Credit : SettlementItem
    {
        public Credit() {}
        public Credit(string settlementId) : base(settlementId){}

        [SheetColumn("PRO #")]
        public string ProNumber { get; set; }

        [SheetColumn("DELV DT")]
        public string DeliveryDate { get; set; }

        [SheetColumn("CARR/DRIVER")]
        public string Driver { get; set; }        
        
        [SheetColumn("CARR INV/TRK")]
        public int TruckId { get; set; }        

        [SheetColumn("RPM")]
        public double RatePerMile { get; set; }        

        [SheetColumn("MILES")]
        public int Miles { get; set; }        

        [SheetColumn("EXT AMOUNT")]
        public double ExtendedAmount { get; set; }        

        [SheetColumn("DETENTION")]
        public double Detention { get; set; }        

        [SheetColumn("DEADHEAD")]
        public double DeadHead { get; set; }      

        [SheetColumn("STOP OFF")]
        public double StopOff { get; set; }      

        [SheetColumn("CANADA")]
        public double Canada { get; set; }      

        [SheetColumn("LAYOVER")]
        public double Layover { get; set; }     

        [SheetColumn("HANDLOAD")]
        public double HandLoad { get; set; }           

        [SheetColumn("TOLLS")]
        public double Tolls { get; set; }                         

        [SheetColumn("BONUS")]
        public double Bonus { get; set; }             

        [SheetColumn("EMPTY")]
        public double Empty { get; set; }             

        [SheetColumn("TOTAL PAID")]
        public double TotalPaid { get; set; }             

        [SheetColumn("CREDIT DATE")]
        public string CreditDate { get; set; }             

        [SheetColumn("CREDIT DESCRIPTION")]
        public string CreditDescriptions { get; set; }             

        [SheetColumn("RATE PER MILE")]
        public string RatePerMileDescription { get; set; }   

        [SheetColumn("CREDIT AMOUNT")]
        public double CreditAmount { get; set; }        

        [SheetColumn("ADV DATE")]
        public string AdvanceDate { get; set; }     
      
        [SheetColumn("ADV DESCRIPTION")]
        public string AdvanceDescription { get; set; }     

        [SheetColumn("ADV #")]
        public string AdvanceNumber { get; set; }    

        [SheetColumn("ADV AMOUNT")]
        public double AdvanceAmount { get; set; }      

        [SheetColumn("OTHER")]
        public double Other { get; set; }     
    }
}