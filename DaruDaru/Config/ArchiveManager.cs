using System;
using System.Collections.Generic;

namespace DaruDaru.Config
{
    internal static class ArchiveManager
    {
        public static HashSet<string> Instance { get; } = new HashSet<string>();

        public static void Remove(string archiveCode)
        {
            lock (Instance)
                Instance.Remove(archiveCode);
        }

        public static void AddDownloaded(string archiveCode)
        {
            lock (Instance)
                Instance.Add(archiveCode);
        }

        public static bool CheckDownloaded(string archiveCode)
        {
            lock (Instance)
                return Instance.Contains(archiveCode);
        }
        public static IEnumerable<TData> CheckNewUrl<TData>(IEnumerable<TData> src, Func<TData, string> keySelector)
        {
            var lst = new List<TData>();

            lock (Instance)
                foreach (var data in src)
                    if (!Instance.Contains(keySelector(data)))
                        lst.Add(data);

            return lst.ToArray();
        }

        public static void Clear()
        {
            lock (Instance)
                Instance.Clear();
        }
    }
}
