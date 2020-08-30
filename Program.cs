﻿using DotnetTaskV4.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DotnetTaskV4
{
    class Program
    {
        static async Task Main()
        {
            var configuration = GetConfiguration();
            var settings = ParseSettings(configuration);
            if (!IsValidSettings(settings, out var validationMessage))
            {
                Console.WriteLine($"Cannot proceed. {validationMessage}");
            }

            Console.WriteLine("Press any key to exit.");
            Console.ReadLine();
        }

        private static IConfigurationRoot GetConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            return builder.Build();
        }

        private static Settings ParseSettings(IConfigurationRoot configuration)
        {
            return new Settings
            {
                DataRootDirectory = configuration["DataRootDirectory"],
                ReportDirectory = configuration["ReportDirectory"]
            };
        }

        private static bool IsValidSettings(Settings settings, out string validationMessage)
        {
            if (!Directory.Exists(settings.DataRootDirectory))
            {
                validationMessage = "Data root directory does not exist.";
                return false;
            }

            if (!Directory.Exists(settings.ReportDirectory))
            {
                validationMessage = "Report destination directory does not exist.";
                return false;
            }

            validationMessage = null;
            return true;
        }
    }
}
