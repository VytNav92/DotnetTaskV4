using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DotnetTaskV4.ReportGenerator
{
    public class ReportGenerator<T>
    {
        private readonly string _delimiter;
        private readonly IReadOnlyCollection<string> _headerColumns;
        private readonly IReadOnlyCollection<Func<T, string>> _dataRowGenerationRules;

        public ReportGenerator(
            string delimiter,
            IReadOnlyCollection<string> headerColumns,
            IReadOnlyCollection<Func<T, string>> dataRowGenerationRules)
        {
            _delimiter = delimiter;
            _headerColumns = headerColumns;
            _dataRowGenerationRules = dataRowGenerationRules;
        }

        public async Task GenerateAsync(string fileName, IEnumerable<T> reportData)
        {
            using var writer = new StreamWriter(new FileStream(fileName, FileMode.Create, FileAccess.Write));
            await writer.WriteLineAsync(string.Join(_delimiter, _headerColumns));

            foreach (var data in reportData)
                await writer.WriteLineAsync(string.Join(_delimiter, _dataRowGenerationRules.Select(rule => rule(data))));
        }
    }
}
