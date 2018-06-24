using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DaruDaru.Core;

namespace DaruDaru.Marumaru
{
    internal static class ArchiveLog
    {
        private static readonly string LogPath = Path.Combine(App.BaseDirectory, "archive.cfg");

        private static readonly SortedSet<string> Codes = new SortedSet<string>();

        static ArchiveLog()
        {
            if (File.Exists(LogPath))
                foreach (var code in File.ReadLines(LogPath, Encoding.UTF8))
                    Codes.Add(code);
        }

        public static void Remove(string archiveCode)
        {
            lock (Codes)
                Codes.Remove(archiveCode);
        }

        public static void AddDownloaded(string archiveCode)
        {
            lock (Codes)
            {
                Codes.Add(archiveCode);

                Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
                using (var file = File.OpenWrite(LogPath))
                using (var writer = new StreamWriter(file, Encoding.UTF8))
                {
                    file.SetLength(0);

                    foreach (var code in Codes)
                        writer.WriteLine(code);

                    writer.Flush();
                    file.Flush();
                }

                File.SetAttributes(LogPath, File.GetAttributes(LogPath) | FileAttributes.Hidden | FileAttributes.System);
            }
        }

        public static bool CheckDownloaded(string archiveCode)
        {
            lock (Codes)
                return Codes.Contains(archiveCode);
        }
        public static IEnumerable<TData> CheckNewUrl<TData>(IEnumerable<TData> src, Func<TData, string> keySelector)
        {
            var lst = new List<TData>();

            lock (Codes)
                foreach (var data in src)
                    if (!Codes.Contains(keySelector(data)))
                        lst.Add(data);

            return lst.ToArray();
        }

        public static void Clear()
        {
            lock (Codes)
                Codes.Clear();
        }
    }
}
