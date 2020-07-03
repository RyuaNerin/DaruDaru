using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DaruDaru.Utilities
{
    internal static class ParallelAsync
    {
        public static Task ForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> action, int tasks)
        {
            return Task.WhenAll(Partitioner.Create(source).GetPartitions(tasks).AsParallel().Select(p => AwaitPartition(p)));

            async Task AwaitPartition(IEnumerator<T> partition)
            {
                using (partition)
                {
                    while (partition.MoveNext())
                    {
                        await action(partition.Current);
                    }
                }
            }
        }
    }
}
