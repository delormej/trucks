using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace Trucks
{
    [System.AttributeUsage(System.AttributeTargets.Property)]
    public class SheetColumnAttribute : System.Attribute
    {
        public string Header;
        public SheetColumnAttribute(string header)
        {
            this.Header = header;
        }
    }

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

    public class Deduction : SettlementItem
    {
        public Deduction() {}
        public Deduction(string settlementId) : base(settlementId){}

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

    public class SettlementItem
    {
        public SettlementItem() {}
        public SettlementItem(string settlementId)
        {
            this.SettlementId = settlementId;
        }

        public string SettlementId { get; set; }    
    }

    public class SettlementHistory
    {
        public SettlementHistory() {}

        public string SettlementId { get; set; }
        public DateTime SettlementDate { get; set; }

        public List<Credit> Credits { get; set; }
        public List<Deduction> Deductions { get; set; }
    }
}