using DotnetTaskV4.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DotnetTaskV4
{
    public class DataProcessor
    {
        private readonly Channel<DeviceData> _channel;

        public DataProcessor()
        {
            _channel = Channel.CreateUnbounded<DeviceData>(new UnboundedChannelOptions
            {
                SingleWriter = false,
                SingleReader = true
            });
        }

        public async Task ProcessAsync(IReadOnlyCollection<string> csvFiles, int maxDegreeOfParallelism, CancellationToken cancellationToken)
        {
            await Task.Yield();
            var consumeTask = ConsumeAsync();

            await csvFiles.RunParallelAsync(async csvFilePath =>
            {
                await ProduceAsync(csvFilePath, cancellationToken);
            }, maxDegreeOfParallelism, cancellationToken);

            _channel.Writer.Complete();
        }

        private async Task ConsumeAsync()
        {
            while (await _channel.Reader.WaitToReadAsync())
            {
                var data = await _channel.Reader.ReadAsync();
            }
        }

        private async Task ProduceAsync(string csvFilePath, CancellationToken cancellationToken)
        {
            using var reader = new StreamReader(csvFilePath);
            await foreach (var data in Parser.ParseAsync(reader, cancellationToken))
                await _channel.Writer.WriteAsync(data);
        }
    }
}
