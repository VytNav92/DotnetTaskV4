using DotnetTaskV4.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DotnetTaskV4
{
    class Program
    {
        private const string CsvFileExtension = ".csv";

        static async Task Main()
        {
            var configuration = GetConfiguration();
            var settings = ParseSettings(configuration);
            if (!IsValidSettings(settings, out var validationMessage))
            {
                Console.WriteLine($"Cannot proceed. {validationMessage}");
            }
            else
            {

                Console.WriteLine("Checking csv files...");
                var csvFiles = Directory.GetFiles(settings.DataRootDirectory, $"*{CsvFileExtension}", SearchOption.AllDirectories);
                Console.WriteLine($"Found {csvFiles.Length} csv files");

                Console.WriteLine($"Processing csv files...");
                using var cancellationTokenSource = new CancellationTokenSource();
                Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e)
                {
                    if (!cancellationTokenSource.IsCancellationRequested)
                    {
                        e.Cancel = true;
                        cancellationTokenSource.Cancel();
                    }
                };

                var dataProcessor = new DataProcessor();
                var dataProcessorTask = dataProcessor.ProcessAsync(csvFiles, Environment.ProcessorCount * 2, cancellationTokenSource.Token);
                await TrackProgress(dataProcessorTask, dataProcessor);

                var reportData = await dataProcessorTask;
                Console.WriteLine($"Total files count: {dataProcessor.ProcessedFilesCount}");
                Console.WriteLine($"Execution time: {dataProcessor.ElapsedTime}");
            }
            Console.WriteLine("Press any key to exit.");
            Console.ReadLine();
        }

        private static async Task TrackProgress(Task dataProcessorTask, DataProcessor dataProcessor)
        {
            var cursorLeftPosition = Console.CursorLeft;
            var cursorTopPosition = Console.CursorTop;
            var consoleCleaner = new string(' ', Console.WindowWidth);

            do
            {
                await Task.Delay(1000);
                Console.SetCursorPosition(cursorLeftPosition, cursorTopPosition);
                Console.Write(consoleCleaner);
                if (dataProcessor.ElapsedTime.TotalMilliseconds > 0)
                {
                    var averageSpeed = 1000 * dataProcessor.ProcessedFilesCount / dataProcessor.ElapsedTime.TotalMilliseconds;
                    Console.SetCursorPosition(cursorLeftPosition, cursorTopPosition);
                    Console.Write($"Average speed: {averageSpeed:0.#} files per second");
                }
            }
            while (!dataProcessorTask.IsCompleted);

            Console.WriteLine();
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
