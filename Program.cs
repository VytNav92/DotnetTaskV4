using DotnetTaskV4.Models;
using DotnetTaskV4.ReportGenerator;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DotnetTaskV4
{
    class Program
    {
        private const string CsvFileExtension = ".csv";
        private const string ReportCsvDelimiter = ";";

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

                Console.WriteLine($"Total files count: {dataProcessor.ProcessedFilesCount}");
                Console.WriteLine($"Execution time: {dataProcessor.ElapsedTime}");
                var reportData = await dataProcessorTask;

                Console.WriteLine("Creating report...");
                await GenerateReportAsync(settings.ReportDirectory, reportData);
                Console.WriteLine("Report created successfully");
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
        private static async Task GenerateReportAsync(string directoryPath, Dictionary<string, ReportData> reportData)
        {
            var reportGenerator = new ReportGenerator<ReportData>(ReportCsvDelimiter, ReportGeneratorRules.HeaderColumnNames, ReportGeneratorRules.DataRowGenerationRules);
            var orderedReportData = reportData.Values.OrderBy(x => x.Band).ThenBy(x => x.Plc);

            var reportFileName = $"report-{DateTime.Now:yyyy-MM-dd_hhmmss}{CsvFileExtension}";
            var reportPath = Path.Combine(directoryPath, reportFileName);
            await reportGenerator.GenerateAsync(reportPath, orderedReportData);
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
