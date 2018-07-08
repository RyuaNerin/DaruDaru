using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using DaruDaru.Core;
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

        private static readonly Regex InvalidRegex = new Regex($"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()))}]", RegexOptions.Compiled);
        public static string ReplaceInvalid(string s) => InvalidRegex.Replace(s, "_");

        public static string ReplcaeHtmlTag(string s) => s.Replace("&nbsp;", " ")
                                                          .Replace("&lt;", "<")
                                                          .Replace("&gt;", ">")
                                                          .Replace("&amp;", "&")
                                                          .Replace("&quot;", "\"")
                                                          .Replace("&apos;", "'")
                                                          .Replace("&copy;", "©")
                                                          .Replace("&reg;", "®");

        public static bool Retry(Func<bool> action, int retries = 3)
        {
            do
            {
                try
                {
                    if (action())
                        return true;
                }
                catch (WebException)
                {
                }
                catch (SocketException)
                {
                }
                catch (Exception ex)
                {
                    CrashReport.Error(ex);
                }

                Thread.Sleep(1000);
            } while (--retries > 0);

            return false;
        }
    }
}
