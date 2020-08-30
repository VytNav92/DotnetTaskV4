using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DotnetTaskV4
{
    public static class EnumerableExtensions
    {
        public static async Task RunParallelAsync<T>(
            this IEnumerable<T> collection,
            Func<T, Task> action,
            int maxDegreeOfParallelism,
            CancellationToken cancellationToken = default)
        {
            var activeTasks = new List<Task>(maxDegreeOfParallelism);

            foreach (var task in collection.Select(action))
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                activeTasks.Add(task);
                if (activeTasks.Count == maxDegreeOfParallelism)
                {
                    await Task.WhenAny(activeTasks.ToArray());
                    activeTasks.RemoveAll(t => t.IsCompleted);
                }
            }

            await Task.WhenAll(activeTasks.ToArray());
        }
    }
}
