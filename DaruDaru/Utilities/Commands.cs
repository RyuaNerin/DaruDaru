using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace DaruDaru.Utilities
{
    internal static class Utility
    {
        public static string GetHoneyView()
        {
            string honeyView = null;

            try
            {
                using (var reg = Registry.CurrentUser.OpenSubKey("Software\\Honeyview"))
                    honeyView = (string)reg.GetValue("ProgramPath");
            }
            catch
            {
            }

            return !string.IsNullOrWhiteSpace(honeyView) && File.Exists(honeyView) ? honeyView : null;
        }

        public static void OpenDir(string directory)
        {
            try
            {
                Process.Start("explorer", $"\"{directory}\"").Dispose();
            }
            catch
            {
            }
        }

        public static void StartProcess(string filename, string arg = null)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = filename, Arguments = arg }).Dispose();
            }
            catch
            {
            }
        }
    }
}
