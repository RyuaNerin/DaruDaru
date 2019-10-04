using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DaruDaru.Marumaru
{
    public class ImageDecryptor
    {
        private readonly int[] m_partOrder;
        private readonly V2 m_v2 = new V2();
        private readonly bool m_skip;

        public ImageDecryptor(string html, Uri uri)
        {
            this.m_v2.Chapter = int.Parse(Regex.Match(uri.Query, "wr_id=(\\d+)").Groups[1].Value);

            var view_cnt = int.Parse(Regex.Match(html, "view_cnt ?= ?(\\d+)").Groups[1].Value);
            if (3e4 < view_cnt / 10)
            {
                this.m_v2._CX = 1;
                this.m_v2._CY = 6;
            }
            else if (2e4 < view_cnt / 10)
            {
                this.m_v2._CX = 1;
            }
            else if (1e4 < view_cnt / 10)
            {
                this.m_v2._CY = 1;
            }

            this.m_v2.__s(view_cnt / 10);

            this.m_partOrder = this.m_v2.GetPagenation();

            this.m_skip = this.m_partOrder.SequenceEqual(this.m_partOrder.OrderBy(e => e));
        }

        public void Decrypt(Stream stream)
        {
            if (this.m_skip)
                return;


            Image imgOriginal;
            ImageFormat imgFormat;

            stream.Position = 0;
            using (var n = Image.FromStream(stream))
            {
                imgOriginal = new Bitmap(n);
                imgFormat = n.RawFormat;
            }

            using (imgOriginal)
            {
                using (var imgDec = new Bitmap(imgOriginal))
                {
                    using (var t = Graphics.FromImage(imgDec))
                    {
                        var partW /* l */ = (int)Math.Floor((double)imgOriginal.Width / this.m_v2._CX);
                        var PartH /* d */ = (int)Math.Floor((double)imgOriginal.Height / this.m_v2._CY);

                        for (var m = 0; m < this.m_partOrder.Length; ++m)
                        {
                            var dstX     /* u */ = m % this.m_v2._CX;
                            var dstY     /* p */ = (int)Math.Floor((double)m / this.m_v2._CX);
                            var srcX     /* g */ = this.m_partOrder[m] % this.m_v2._CX;
                            var srcY     /* _ */ = (int)Math.Floor((double)this.m_partOrder[m] / this.m_v2._CX);

                            t.DrawImage(
                                imgOriginal,
                                new Rectangle(srcX * partW, srcY * PartH, partW, PartH),
                                new Rectangle(dstX * partW, dstY * PartH, partW, PartH),
                                GraphicsUnit.Pixel);
                        }
                    }

                    stream.SetLength(0);
                    imgDec.Save(stream, imgFormat);
                }
            }
        }

        class V2
        {
            public int Chapter { get; set; }

            public int _CX { get; set; } = 5;
            public int _CY { get; set; } = 5;
            public int __seed { get; set; }


            public void __s(int view_cnt)
                => this.__seed = view_cnt;

            public int __v
            {
                get
                {
                    if (this.Chapter < 554714)
                    {
                        var e = 1e4 * Math.Sin(this.__seed++);
                        return (int)Math.Floor(1e5 * (e - Math.Floor(e)));
                    }

                    this.__seed++;

                    var t = 100 * Math.Sin(10 * this.__seed);
                    var n = 1e3 * Math.Cos(13 * this.__seed);
                    var a = 1e4 * Math.Tan(14 * this.__seed);

                    t = Math.Floor(100 * (t - Math.Floor(t)));
                    n = Math.Floor(1e3 * (n - Math.Floor(n)));
                    a = Math.Floor(1e4 * (a - Math.Floor(a)));

                    return (int)t + (int)n + (int)a;
                }
            }

            public int[] GetPagenation()
            {
                var lst = new List<Tuple<int, int>>();

                if (this.__seed == 0)
                {
                    for (var s = 0; s < this._CX * this._CY; s++)
                        lst.Add(new Tuple<int, int>(s, s));
                }
                else
                {
                    for (var s = 0; s < this._CX * this._CY; s++)
                        lst.Add(new Tuple<int, int>(s, this.__v));
                }

                lst.Sort((a, b) => a.Item2 != b.Item2 ? a.Item2 - b.Item2 : a.Item1 - b.Item1);

                return lst.Select(e => e.Item1).ToArray();
            }

        }
    }
}
