using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace DaruDaru.Utilities
{
    internal sealed class HoneyViwer
    {
        public static bool TryCreate(out HoneyViwer honeyViwer)
        {
            try
            {
                using (var reg = Registry.CurrentUser.OpenSubKey("Software\\Honeyview"))
                {
                    var hvPath = (string)reg.GetValue("ProgramPath");

                    if (File.Exists(hvPath))
                    {
                        honeyViwer = new HoneyViwer(hvPath);
                        return true;
                    }
                }
            }
            catch
            {
            }

            honeyViwer = null;
            return false;
        }

        public HoneyViwer(string path)
        {
            this.m_path = path;
        }

        private readonly string m_path;

        public void Open(string path)
        {
            if (!File.Exists(path))
                return;

            try
            {
                Process.Start(this.m_path, $"\"{path}\"").Dispose();
            }
            catch
            {
            }
        }
    }
}
