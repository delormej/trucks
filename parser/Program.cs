﻿using System;
using System.IO;

namespace Trucks
{
    class Program
    {
        static void Main(string[] args)
        {
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
                parser.LoadFromCsv(csv); 
            }
        }

        private static void ShowUsage()
        {
            Console.WriteLine("parser <input.csv>");
        }
    }
}
