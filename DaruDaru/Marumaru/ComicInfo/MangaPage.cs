using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DaruDaru.Config;
using DaruDaru.Core;
using DaruDaru.Core.Windows;
using DaruDaru.Utilities;
using HtmlAgilityPack;
using ICSharpCode.SharpZipLib.Zip;
using Sentry;

namespace DaruDaru.Marumaru.ComicInfo
{
    internal class MangaPage : Comic
    {
        /// <summary>정상적으로 작동한 Cdn 우선으로 다운로드 시도하게 하기 위해서</summary>
        private static readonly SortedDictionary<string, int> CdnScore = new SortedDictionary<string, int>
        {
            { "blogspot.com",  -50 },
            { "google.com",    -50 },
            { "filecdn.xyz",  -100 },
        };
        private const int CdnScoreDefault = 5;

        private const int CDNPeekCount = 8;

        public MangaPage(bool addNewOnly, Uri uri, string title, string tempTitleWithNo = null)
            : base(addNewOnly, DaruUriParser.Manga.FixUri(uri), title)
        {
            this.TitleWithNo = tempTitleWithNo;

            var entry = ArchiveManager.GetManga(this.ArchiveCode);
            if (entry != null)
            {
                this.TitleWithNo = entry.TitleWithNo;
                this.ZipPath     = entry.ZipPath;
                this.State       = MaruComicState.Complete_2_Archived;

                MainWindow.Instance.WakeThread();
            }
        }

        private ImageInfomation[] m_images;
        private ImageDecryptor m_decryptor;
        
        public string ZipPath { get; set; }

        public string ArchiveCode => DaruUriParser.Manga.GetCode(this.Uri);
        
        [DebuggerDisplay("{Index} - Count: {ImageUri?.Length} - Ext : {Extension}")]
        private class ImageInfomation
        {
            public int          Index;
            public ImageUri[]   ImageUri;
            public FileStream   TempStream;
            public string       Extension;
        }
        [DebuggerDisplay("{Uri}")]
        private struct ImageUri
        {
            public Uri Uri;
            public bool Downloaded;
        }
        private class MangaInfomation
        {
            public List<ImageInfomation> Images { get; } = new List<ImageInfomation>();
            public Uri                   NewUri;
        }

        protected override bool GetInfomationPriv(HttpClientEx hc, ref int count)
        {
            MangaInfomation mangaInfo = null;
            HttpStatusCode statusCode = 0;

            var retrySuccess = Utility.Retry((retries) => (mangaInfo = this.GetInfomationWorker(hc, retries, out statusCode)) != null);

            if (!retrySuccess)
            {
                this.SetStatusFromHttpStatusCode(statusCode);
                return false;
            }

            this.Uri = mangaInfo.NewUri;

            this.ProgressValue = 0;
            this.ProgressMaximum = mangaInfo.Images.Count;

            this.m_images = mangaInfo.Images.ToArray();

            // 다운로드 시작
            this.State = MaruComicState.Working_2_WaitDownload;

            count = 1;

            return true;
        }

        private static readonly Random Random = new Random(DateTime.Now.Millisecond);
        private MangaInfomation GetInfomationWorker(HttpClientEx hc, int retries, out HttpStatusCode statusCode)
        {
            var mangaInfo = new MangaInfomation();

            var doc = new HtmlDocument();
            string html;

            using (var res = hc.GetAsync(this.Uri).GetAwaiter().GetResult())
            {
                statusCode = res.StatusCode;
                if (this.WaitFromHttpStatusCode(retries, statusCode))
                    return null;

                html = res.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                if (string.IsNullOrWhiteSpace(html))
                    return null;

                mangaInfo.NewUri = res.RequestMessage.RequestUri ?? this.Uri;
            }

            doc.LoadHtml(html);

            #region 폴더 이름은 Detail 에서 설정
            {
                // /bbs/page.php?hid=manga_detail&manga_id=9495
                var toonNav = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'toon-nav')]");
                if (toonNav == null)
                    return null;

                string detailCode = null;
                foreach (HtmlNode node in toonNav.SelectNodes(".//a[@href]").ToArray())
                {
                    var href = node.GetAttributeValue("href", "");
                    if (string.IsNullOrWhiteSpace(href))
                        continue;

                    var code = DaruUriParser.Detail.GetCode(new Uri(this.Uri, href));
                    if (string.IsNullOrWhiteSpace(code))
                        continue;

                    detailCode = code;
                    break;
                }

                var detailEntry = ArchiveManager.GetDetail(detailCode);

                if (detailEntry == null)
                {
                    var detailUri = DaruUriParser.Detail.GetUri(detailCode);
                    
                    DetailPage.DetailInfomation detailInfo = null;

                    var detailResult = Utility.Retry((retries2) =>
                    {
                        using (var res = hc.GetAsync(detailUri).GetAwaiter().GetResult())
                        {
                            if (this.WaitFromHttpStatusCode(retries2, res.StatusCode))
                                return false;

                            var htmlDetail = res.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                            if (string.IsNullOrWhiteSpace(html))
                                return false;

                            detailInfo = DetailPage.GetDetailInfomation(hc, detailUri, htmlDetail);
                            return detailInfo != null;
                        }
                    });

                    if (!detailResult)
                        return null;

                    ArchiveManager.UpdateDetail(detailCode, detailInfo.Title, detailInfo.MangaList.Select(e => e.MangaCode).ToArray());

                    detailEntry = ArchiveManager.GetDetail(detailCode);

                    if (detailEntry == null)
                        return null;
                }

                this.Title = detailEntry.Title;
            }
            #endregion

            #region Detail.Title + xx화
            {
                var titleNode = doc.DocumentNode.SelectSingleNode(".//div[contains(@class, 'toon-title')]");
                if (titleNode == null)
                    return null;

                foreach (var titleNodeChild in titleNode.ChildNodes.ToArray())
                {
                    if (titleNodeChild.NodeType == HtmlNodeType.Element)
                        titleNodeChild.Remove();
                }

                var title = Utility.ReplaceHtmlTagAndRemoveTab(titleNode.InnerText ?? string.Empty);
                if (string.IsNullOrWhiteSpace(title))
                    return null;

                if (string.IsNullOrWhiteSpace(title))
                    return null;
                this.TitleWithNo = title;
            }
            #endregion

            #region 이미지 정보 가져오는 부분
            {
                var chapter = int.Parse(Regex.Match(doc.DocumentNode.InnerHtml, "var *chapter *= *(\\d+)")?.Groups[1].Value ?? "0");

                var catchArr = new Func<string, IEnumerable<string>>(e =>
                {
                    var m = Regex.Match(doc.DocumentNode.InnerHtml, $"var {e} *= *((?:[^;\\\\]|\\\\.)*)");
                    if (!m.Success)
                        return new string[0];

                    return Regex.Matches(m.Groups[1].Value, @"""((?:[^""\\]|\\.)*)""")
                          .Cast<Match>()
                          .Select(le => Regex.Replace(le.Groups[1].Value, @"\\(.)", lm => lm.Groups[1].Value));
                });

                var cdnList = catchArr("cdn_domains")
                    .ToArray();

                var imgList = catchArr("img_list")
                    .Select(e => new Uri(this.Uri, e + "?quick"))
                    .ToArray();

                var imgList1 = catchArr("img_list1")
                    .Select(e => new Uri(this.Uri, e))
                    .ToArray();

                var lst = new HashSet<Uri>(new UriIEqualityComparer());
                for (var i = 0; i < imgList.Length; i++)
                {
                    lst.Clear();

                    var addList = new Action<Uri>(uri =>
                    {
                        var host = uri.Host;

                        if (cdnList?.Length > 0)
                        {
                            if (host.Contains("cdntigermask.xyz") || host.Contains("cdnmadmax.xyz") || host.Contains("filecdn.xyz"))
                            {
                                lst.Add(new UriBuilder(uri) { Host = cdnList[(chapter + 4 * i) % cdnList.Length] }.Uri);

                                for (var cdnPeek = 0; cdnPeek < CDNPeekCount; cdnPeek++)
                                {
                                    lst.Add(new UriBuilder(uri) { Host = cdnList[Random.Next(cdnList.Length)] }.Uri);
                                    lst.Add(new UriBuilder(uri) { Host = cdnList[Random.Next(cdnList.Length)] }.Uri);
                                    lst.Add(new UriBuilder(uri) { Host = cdnList[Random.Next(cdnList.Length)] }.Uri);
                                }
                            }
                        }

                        lst.Add(imgList[i]);

                        if (!host.Contains("google.") | !host.Contains("blogspot."))
                        {
                            lst.Add(new UriBuilder(uri) { Host = "s3." + host }.Uri);
                            lst.Add(new UriBuilder(uri) { Host = host.Replace("img.", "s3.") }.Uri);
                        }
                    });

                    if (i < imgList1.Length)
                        addList(imgList1[i]); 

                    addList(imgList[i]);

                    var imageInfo = new ImageInfomation()
                    {
                        Index    = i + 1,
                        ImageUri = lst.Distinct().Select(e => new ImageUri { Uri = e }).ToArray(),
                    };

                    mangaInfo.Images.Add(imageInfo);
                }
            }
            #endregion

            this.m_decryptor = new ImageDecryptor(html, mangaInfo.NewUri);

            return mangaInfo;
        }

        protected override void StartDownloadPriv(HttpClientEx hc)
        {
            this.ZipPath = Path.Combine(new DirectoryInfo(Path.Combine(this.ConfigCur.SavePath, Utility.ReplaceInvalid(this.Title))).FullName, Utility.ReplaceInvalid(this.TitleWithNo) + ".zip");

            string tempPath = null;

            try
            {
                foreach (var e in this.m_images)
                    e.TempStream = new FileStream(Path.GetTempFileName(), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, App.BufferSize, FileOptions.DeleteOnClose);

                if (!this.Download(hc))
                {
                    if (this.m_images.Any(e => e.Extension != null))
                    {
                        this.ToolTip = string.Format(
                            "{0} / {1}\n누락된 페이지 : {2}",
                            this.m_images.Count(e => e.Extension != null),
                            this.m_images.Length,
                            string.Join(", ", this.m_images.Where(e => e.Extension == null).Select(e => e.Index))
                        );
                        this.State = MaruComicState.Error_7_MissingPage;
                    }
                    else
                        this.State = MaruComicState.Error_1_Error;
                    return;
                }

                tempPath = Path.GetTempFileName();

                this.State = MaruComicState.Working_4_Compressing;
                this.Compress(tempPath);

                bool fileMode = true;
                if (File.Exists(this.ZipPath))
                {
                    string comment = null;

                    try
                    {
                        using (var fs = new FileStream(this.ZipPath, FileMode.Open, FileAccess.Read))
                        using (var zf = new ZipFile(fs))
                        {
                            if (!string.IsNullOrWhiteSpace(zf.ZipFileComment))
                                comment = zf.ZipFileComment;
                        }
                    }
                    catch
                    {
                    }

                    if (
                        new FileInfo(this.ZipPath).Length == new FileInfo(tempPath).Length ||
                        (
                            comment != null &&
                            Utility.TryCreateUri(comment.Split('\n')[0], out Uri uri) &&
                            DaruUriParser.Manga.GetCode(uri) == this.ArchiveCode
                        ))
                    {
                        fileMode = false;
                        this.State = MaruComicState.Complete_2_Archived;
                    }
                }
                if (fileMode)
                {
                    this.ZipPath = MoveFile(tempPath, this.ZipPath);
                    this.SpeedOrFileSize = Utility.ToEICFormat(new FileInfo(this.ZipPath).Length);
                    this.State = MaruComicState.Complete_1_Downloaded;
                }
                
                // 디렉토리 수정시간 업데이트
                Directory.SetCreationTime(this.ZipPath, DateTime.Now);
                Directory.SetLastWriteTime(this.ZipPath, DateTime.Now);
                Directory.SetLastAccessTime(this.ZipPath, DateTime.Now);
                
                ArchiveManager.UpdateManga(this.ArchiveCode, this.TitleWithNo, this.ZipPath);
            }
            catch (Exception ex)
            {
                this.SpeedOrFileSize = null;
                this.State = MaruComicState.Error_1_Error;

                SentrySdk.CaptureException(ex);
            }
            finally
            {
                if (this.m_images != null)
                {
                    foreach (var file in this.m_images)
                        if (file.TempStream != null)
                            file.TempStream.Dispose();

                    this.m_images = null;
                }

                try
                {
                    File.Delete(tempPath);
                }
                catch
                {
                }
            }
        }

        private const int HR_ERROR_FILE_EXISTS = unchecked((int)0x80070050);
        private static string MoveFile(string orig, string dest)
        {
            var dir = Directory.CreateDirectory(Path.GetDirectoryName(dest)).FullName;
            var baseName = Path.GetFileNameWithoutExtension(dest);
            var ext = Path.GetExtension(dest);

            dest = Path.Combine(dir, Path.GetFileName(dest));

            var i = 2;

            while (File.Exists(dest))
            {
                dest = Path.Combine(
                    dir,
                    string.Format(
                        "{0} ({1}){2}",
                        baseName,
                        i++,
                        ext
                    )
                );
            }
            
            File.Move(orig, dest);

            return dest;
        }

        private long m_downloaded;
        private bool Download(HttpClientEx hc)
        {
            using (var stopSlim = new ManualResetEventSlim(false))
            {
                this.m_downloaded = 0;
                var updateTask = Task.Factory.StartNew(() =>
                {
                    var startTime = DateTime.Now;

                    double befSpeed = 0;
                    while (!stopSlim.IsSet)
                    {
                        befSpeed = (befSpeed + Interlocked.Exchange(ref this.m_downloaded, 0) / (DateTime.Now - startTime).TotalSeconds) / 2;

                        if (double.IsNaN(befSpeed))
                            befSpeed = 0;

                        this.SpeedOrFileSize = Utility.ToEICFormat(befSpeed, "/s");

                        Thread.Sleep(500);
                    }
                });

                var parallelOption = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 8,
                };

                using (var cts = new CancellationTokenSource())
                {
                    ParallelAsync.ForEachAsync(
                        this.m_images,
                        async e => await this.DownloadImage(e, hc, cts.Token, cts.Cancel),
                        8).GetAwaiter().GetResult();

                    stopSlim.Set();
                    updateTask.Wait();

                    if (!this.IgnoreErrorMissingPage && cts.IsCancellationRequested)
                        return false;
                }
            }

            this.SpeedOrFileSize = null;

            // 모든 이미지가 다운로드가 완료되어야 함
            return this.IgnoreErrorMissingPage || this.m_images.All(e => e.Extension != null);
        }

        private async Task DownloadImage(ImageInfomation e, HttpClientEx hc, CancellationToken cancellationToken, Action cancel)
        {
            for (var index = 0; index < e.ImageUri.Length && !cancellationToken.IsCancellationRequested; ++index)
            {
                lock (CdnScore)
                {
                    Array.Sort(
                        e.ImageUri,
                        (a, b) => GetScore(a.Uri.Host).CompareTo(GetScore(b.Uri.Host)) * -1
                    );
                }

                for (var i = 0; i < e.ImageUri.Length && !cancellationToken.IsCancellationRequested; ++i)
                {
                    if (e.ImageUri[i].Downloaded)
                        continue;

                    var succ = await Utility.RetryAsync(async (retries) => await this.DownloadWorker(hc, e, e.ImageUri[i].Uri, cancellationToken), 1);
                    e.ImageUri[i].Downloaded = true;

                    var h = e.ImageUri[i].Uri.Host;
                    lock (CdnScore)
                    {
                        CdnScore[h] = (CdnScore.TryGetValue(h, out var c) ? c : CdnScoreDefault) + (succ ? 1 : -2);
                    }

                    if (succ)
                        return;
                }
            }

            if (!this.IgnoreErrorMissingPage)
                cancel();

            int GetScore(string host)
            {
                if (CdnScore.TryGetValue(host, out var v))
                    return v;

                while (true)
                {
                    var dot = host.IndexOf('.');
                    if (dot == -1)
                        break;

                    var dotNext = host.IndexOf('.', dot + 1);
                    if (dotNext == -1)
                        break;

                    host = host.Substring(dot + 1);

                    if (CdnScore.TryGetValue(host, out v))
                        return v;
                }

                return CdnScoreDefault;
            }
        }

        private async Task<bool> DownloadWorker(HttpClientEx hc, ImageInfomation e, Uri uri, CancellationToken cancellationToken)
        {
            using (var req = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                req.Headers.Referrer = this.Uri;

                using (var res = await hc.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                {
                    if ((int)res.StatusCode / 100 != 2)
                        return false;

                    if (res.Headers.Server.FirstOrDefault()?.ToString()?.Contains("ddos-guard") ?? false)
                        return false;

                    using (var stream = await res.Content.ReadAsStreamAsync())
                    {
                        e.TempStream.SetLength(0);
                        using (var fileWriter = new StreamWithNotify(e.TempStream, count => Interlocked.Add(ref this.m_downloaded, count)))
                        {
                            await stream.CopyToAsync(fileWriter, 4096, cancellationToken);
                        }
                    }
                }

                e.TempStream.Flush();

                e.TempStream.Position = 0;
                e.Extension = Signatures.GetExtension(e.TempStream);

                if (e.Extension == null)
                    return false;

                // 이미지 암호화 푸는 작업
                this.m_decryptor.Decrypt(e.TempStream);
            }

            this.IncrementProgress();
            return true;
        }

        private void Compress(string tempPath)
        {
            var padLength = Math.Min(3, this.m_images.Length.ToString().Length);

            using (var zipFile = new FileStream(tempPath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            using (var zipStream = new ZipOutputStream(zipFile))
            {
                zipFile.SetLength(0);

                if (ConfigManager.Instance.AddComment)
                {
                    zipStream.SetComment(this.Uri.AbsoluteUri + "\nby DaruDaru");
                    zipStream.SetLevel(0);
                }

                var buff = new byte[App.BufferSize];
                int read;
                foreach (var file in this.m_images)
                {
                    if (file.Extension == null)
                        continue;

                    var entry = new ZipEntry(file.Index.ToString().PadLeft(padLength, '0') + file.Extension);

                    zipStream.PutNextEntry(entry);

                    file.TempStream.Seek(0, SeekOrigin.Begin);
                    while ((read = file.TempStream.Read(buff, 0, App.BufferSize)) > 0)
                        zipStream.Write(buff, 0, read);
                    zipStream.Flush();
                }

                zipFile.Flush();
            }
        }
    }
}
