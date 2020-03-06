using System;
using System.Collections.Generic;

namespace Trucks
{
    public class Driver
    {
        public string Name { get; set; }
        public string StreetAddress { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
        public double BasePercent { get; set; }
        public double AccessorialPercent { get; set; }
        public string SocialSecurityNumber { get; set; }
    }

    public class Truck
    {
        // Mapping for cosmos.
        public string id => TruckId;        
        public string TruckId { get; set; }
        public double InitialEquipmentValue { get; set; }
        public double LeasePayment { get; set; }

        public List<TruckInService> InServiceHistory { get; set; }
    }

    public class TruckInService
    {
        public DateTime InServiceDate { get; set; }
        public DateTime OutServiceDate { get; set; }
        public Driver Driver { get; set; }
    }    
}