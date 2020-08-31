using DotnetTaskV4.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DotnetTaskV4
{
    public class DataProcessor
    {
        private readonly Channel<DeviceData> _channel;
        private readonly Stopwatch _stopwatch = new Stopwatch();

        private int _processedFilesCount;

        public int ProcessedFilesCount { get { return _processedFilesCount; } }
        public TimeSpan ElapsedTime { get { return _stopwatch.Elapsed; } }

        public DataProcessor()
        {
            _channel = Channel.CreateUnbounded<DeviceData>(new UnboundedChannelOptions
            {
                SingleWriter = false,
                SingleReader = true
            });
        }

        public async Task<Dictionary<string, ReportData>> ProcessAsync(IReadOnlyCollection<string> csvFiles, int maxDegreeOfParallelism, CancellationToken cancellationToken)
        {
            await Task.Yield();
            var consumeTask = ConsumeAsync();

            _stopwatch.Start();
            await csvFiles.RunParallelAsync(async csvFilePath =>
            {
                await ProduceAsync(csvFilePath, cancellationToken);
                Interlocked.Increment(ref _processedFilesCount);
            }, maxDegreeOfParallelism, cancellationToken);
            _stopwatch.Stop();

            _channel.Writer.Complete();
            return await consumeTask;
        }

        private async Task<Dictionary<string, ReportData>> ConsumeAsync()
        {
            var report = new Dictionary<string, ReportData>();

            while (await _channel.Reader.WaitToReadAsync())
            {
                var data = await _channel.Reader.ReadAsync();

                var key = $"{data.Band}{DeviceData.DataDelimiter}{data.Plc}";

                if (!report.TryGetValue(key, out ReportData reportData))
                {
                    reportData = new ReportData
                    {
                        Band = data.Band,
                        Plc = data.Plc
                    };

                    report[key] = reportData;
                }

                if (data.TxPower < data.MinPower)
                    reportData.LowTxPowerSum += data.TxPower;
                else
                {
                    if (data.TxPower >= data.MinPower && data.TxPower <= data.MaxPower)
                        reportData.InRangeTxPowerSum += data.TxPower;

                    if (data.TxPower >= data.MaxPower)
                        reportData.HighTxPowerSum += data.TxPower;
                }

                if (data.CheckResult)
                    reportData.PassCount++;
                else
                    reportData.FailCount++;
            }

            return report;
        }

        private async Task ProduceAsync(string csvFilePath, CancellationToken cancellationToken)
        {
            using var reader = new StreamReader(csvFilePath);
            await foreach (var data in Parser.ParseAsync(reader, cancellationToken))
                await _channel.Writer.WriteAsync(data);
        }
    }
}
