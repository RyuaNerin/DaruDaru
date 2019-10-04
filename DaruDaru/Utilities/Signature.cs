using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DaruDaru.Utilities
{
    internal static class Signatures
    {
        struct SigInfo
        {
            public SigInfo(string extension, params string[] signatures)
            {
                this.Extension = extension;
                this.Signature = new short[signatures.Length][];

                int i, j;
                for (i = 0; i < signatures.Length; ++i)
                {
                    signatures[i] = RegexFilter.Replace(signatures[i], "");

                    var buff = new short[signatures[i].Length / 2];
                    for (j = 0; j < buff.Length; ++j)
                    {
                        var part = signatures[i].Substring(j * 2, 2);
                        if (part == "??")
                            buff[j] = -1;
                        else
                            buff[j] = byte.Parse(part, NumberStyles.HexNumber);
                    }

                    this.Signature[i] = buff;
                }

                Array.Sort(this.Signature, (a, b) => a.Length.CompareTo(b.Length) * -1);
            }

            public string Extension;
            public short[][] Signature;
        }

        private static readonly Regex RegexFilter = new Regex(@"[^a-fA-F0-9\?]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly SigInfo[] Signature = new SigInfo[]
        {
            new SigInfo(".bmp",  "42 4D"),
            new SigInfo(".jpg",  "FF D8 FF E0 ?? ?? 4A 46 49 46 00 01",
                                 "FF D8 FF E1 ?? ?? 45 78 69 66 00 00",
                                 "FF D8 FF DB"),
            new SigInfo(".png",  "89 50 4E 47 0D 0A 1A 0A"),
            new SigInfo(".gif",  "47 49 46 38 37 61",
                                 "47 49 46 38 39 61"),
            new SigInfo(".tif",  "49 49 2A 00",
                                 "4D 4D 00 2A"),
            new SigInfo(".webp", "52 49 46 46 ?? ?? ?? ?? 57 45 42 50"),
        };
        private static readonly int MaxLength;

        static Signatures()
        {
            MaxLength = Signature.Select(e => e.Signature.Select(ee => ee.Length).Max()).Max();

            Array.Sort(Signature, (a, b) => a.Signature.Select(e => e.Length).Max().CompareTo(b.Signature.Select(e => e.Length).Max()) * -1);
        }

        public static string GetExtension(Stream stream)
        {
            var buff = new byte[MaxLength];
            stream.Read(buff, 0, buff.Length);

            foreach (var ext in Signature)
            {
                for (int i = 0; i < ext.Signature.Length; ++i)
                    if (CheckSignature(buff, ext.Signature[i]))
                        return ext.Extension;
            }

            return null;
        }
        public static bool CheckSignature(byte[] buff, short[] signature)
        {
            for (int i = 0; i < signature.Length; ++i)
                if (signature[i] != -1 && signature[i] != buff[i])
                    return false;

            return true;
        }
    }
}
