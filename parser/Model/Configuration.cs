using System;
using System.Linq;

namespace Trucks
{
    public class ParserConfiguration
    {
        public string ZamzarKey { get; set; }
        public string CosmosDbKey { get; set; }

        public PantherConfiguration[] Panther { get; set; }   
        public string GetPantherPassword(string company)
        {
            PantherConfiguration config = Panther.Where(p => p.Company == company).First();
            if (config == null)
                throw new ArgumentException($"{company} has no panther configuration.");
            return config.Password;
        }
    }

    public class PantherConfiguration
    {
        public string Company { get; set; }
        public string Password { get; set; }
    }    
}