using System;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Trucks
{
    public class ParserConfiguration
    {
        public string ZamzarKey { get; set; }
        public DatabaseConfiguration Database { get; set; }
        public PantherConfiguration[] Panther { get; set; }   
        public string GetPantherPassword(string company)
        {
            PantherConfiguration config = Panther.Where(p => p.Company == company).First();
            if (config == null)
                throw new ArgumentException($"{company} has no panther configuration.");
            return config.Password;
        }

        public static ParserConfiguration Load()
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            ParserConfiguration parserConfig = new ParserConfiguration();
            config.Bind("parser", parserConfig);    
            return parserConfig;        
        }
    }

    public class PantherConfiguration
    {
        public string Company { get; set; }
        public string Password { get; set; }
    }    

    public class DatabaseConfiguration
    {
        public string CosmosDbKey { get; set; }
        public string DatabaseId { get; set; }
        public string EndPointUrl { get; set; }
    }
}