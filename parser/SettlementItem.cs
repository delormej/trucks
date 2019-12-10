using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace Trucks
{
    [System.AttributeUsage(System.AttributeTargets.Property)]
    public class SheetColumnAttribute : System.Attribute
    {
        public string Column, Header;
        public SheetColumnAttribute(string column, string header)
        {
            this.Column = column;
            this.Header = header;
        }
    }

    public class Credit : SettlementItem
    {
        public Credit() {}
        public Credit(string settlementId) : base(settlementId){}

        [SheetColumn("A", "PRO #")]
        public string ProNumber { get; set; }

        [SheetColumn("B", "DELV DT")]
        public string DeliveryDate { get; set; }

        [SheetColumn("C", "CARR/DRIVER")]
        public string Driver { get; set; }        
        
        [SheetColumn("D", "CARR INV/TRK")]
        public int TruckId { get; set; }        

        [SheetColumn("E", "RPM")]
        public double RatePerMile { get; set; }        

        [SheetColumn("F", "MILES")]
        public int Miles { get; set; }        

        [SheetColumn("G", "EXT AMOUNT")]
        public double ExtendedAmount { get; set; }        

//						DETENTION	DEADHEAD	STOP OFF	CANADA	LAYOVER	HANDLOAD	TOLLS	BONUS	EMPTY	TOTAL PAID	CREDIT DATE	CREDIT DESCRIPTION	RATE PER MILE	CREDIT AMOUNT	ADV DATE	ADV DESCRIPTION	ADV #	ADV AMOUNT	OTHER


    }

    public class Deduction : SettlementItem
    {
        public Deduction() {}
        public Deduction(string settlementId) : base(settlementId){}

        [SheetColumn("A", "DATE")]
        public string Date { get; set; }

        [SheetColumn("B", "CARR/DRIVER")]
        public string Driver { get; set; }

        [SheetColumn("C", "CARR INV/TRK")]
        public int TruckId { get; set; }

        [SheetColumn("D", "DESCRIPTION")]
        public string Description { get; set; }        

        [SheetColumn("E", "DEDUCTION AMOUNT")]
        public double Amount { get; set; }    

        [SheetColumn("F", "TOTAL DEDUCTIONS")]
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