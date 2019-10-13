using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DaruDaru.Config;
using DaruDaru.Core;
using DaruDaru.Core.Windows;
using DaruDaru.Utilities;
using HtmlAgilityPack;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;

namespace DaruDaru.Marumaru.ComicInfo
{
    internal class MangaPage : Comic
    {
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

                MainWindow.Instance.WakeDownloader(1);
            }
        }

        private ImageInfomation[] m_images;
        private ImageDecryptor m_decryptor;
        
        public string ZipPath { get; set; }

        public string ArchiveCode => DaruUriParser.Manga.GetCode(this.Uri);
        
        private class ImageInfomation
        {
            public int          Index;
            public Uri[]        ImageUri;
            public FileStream   TempStream;
            public string       Extension;
        }
        private class MangaInfomation
        {
            public List<ImageInfomation> Images { get; } = new List<ImageInfomation>();
            public Uri                   NewUri;
        }

        protected override bool GetInfomationPriv(ref int count)
        {
            MangaInfomation mangaInfo = null;

            using (var wc = WebClientEx.GetOrCreate())
            {
                var retrySuccess = Utility.Retry(() => (mangaInfo = this.GetInfomationWorker(wc)) != null);

                if (!retrySuccess)
                {
                    this.SetStateFromWebClientEx(wc);
                    return false;
                }
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

        private MangaInfomation GetInfomationWorker(WebClientEx wc)
        {
            var mangaInfo = new MangaInfomation();

            var html = this.GetHtml(wc, this.Uri);
            if (html == null)
                return null;

            mangaInfo.NewUri = wc.ResponseUri ?? this.Uri;

            var doc = new HtmlDocument();
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
                    string htmlDetail = null;
                    
                    DetailPage.DetailInfomation detailInfo = null;

                    var detailResult = Utility.Retry(() =>
                    {
                        htmlDetail = this.GetHtml(wc, detailUri);
                        if (htmlDetail == null)
                            return false;
                        
                        detailInfo = DetailPage.GetDetailInfomation(detailUri, htmlDetail);
                        return detailInfo != null;
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
                var imgList = Regex.Matches(Regex.Match(doc.DocumentNode.InnerHtml, "var img_list = [^;]+").Groups[0].Value, "\"([^\"]+)\"")
                          .Cast<Match>()
                          .Select(e => e.Groups[1].Value.Replace("\\", ""))
                          .Select(e => new Uri(this.Uri, e))
                          .ToArray();
                var imgList1 = Regex.Matches(Regex.Match(html, "var img_list1 = [^;]+").Groups[0].Value, "\"([^\"]+)\"")
                          .Cast<Match>()
                          .Select(e => e.Groups[1].Value.Replace("\\", ""))
                          .Select(e => new Uri(this.Uri, e))
                          .ToArray();

                for (var i = 0; i < imgList.Length; i++)
                {
                    var imageInfo = new ImageInfomation()
                    {
                        Index = i + 1,
                    };

                    if (i < imgList1.Length)
                    {
                        imageInfo.ImageUri = new Uri[]
                        {
                            new Uri(imgList[i].ToString().Replace("//img.", "//s3.")),
                            imgList[i],
                            imgList1[i],
                        }.Distinct().ToArray();
                    }
                    else
                    {
                        imageInfo.ImageUri = new Uri[]
                        {
                            new Uri(imgList[i].ToString().Replace("//img.", "//s3.")),
                            imgList[i],
                        }.Distinct().ToArray();
                    }

                    mangaInfo.Images.Add(imageInfo);
                }
            }
            #endregion

            this.m_decryptor = new ImageDecryptor(html, mangaInfo.NewUri);

            return mangaInfo;
        }

        protected override void StartDownloadPriv()
        {
            this.ZipPath = Path.Combine(new DirectoryInfo(Path.Combine(this.ConfigCur.SavePath, Utility.ReplaceInvalid(this.Title))).FullName, Utility.ReplaceInvalid(this.TitleWithNo) + ".zip");

            string tempPath = null;

            try
            {
                foreach (var e in this.m_images)
                    e.TempStream = new FileStream(Path.GetTempFileName(), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, App.BufferSize, FileOptions.DeleteOnClose);

                if (!this.Download())
                {
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
                    MoveFile(tempPath, this.ZipPath);
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

                CrashReport.Error(ex);
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
            dest = Path.Combine(dir, Path.GetFileName(dest));

            try
            {
                File.Move(orig, dest);
            }
            catch (IOException ioex) when (ioex.HResult == HR_ERROR_FILE_EXISTS)
            {
                var i = 2;
                string newZipPath;

                do
                {
                    newZipPath = Path.Combine(
                        dir,
                        string.Format(
                            "{0} ({1}){2}",
                            Path.GetFileNameWithoutExtension(dest),
                            i++,
                            Path.GetExtension(dest))
                        );

                    try
                    {
                        File.Move(orig, newZipPath);
                        dest = newZipPath;
                        break;
                    }
                    catch (IOException ioex2) when (ioex2.HResult == HR_ERROR_FILE_EXISTS)
                    {
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                } while (true);
            }
            catch (Exception e)
            {
                throw e;
            }

            return dest;
        }

        private long m_downloaded;
        private bool Download()
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
                        Thread.Sleep(500);

                        befSpeed = (befSpeed + Interlocked.Read(ref this.m_downloaded) / (DateTime.Now - startTime).TotalSeconds) / 2;

                        this.SpeedOrFileSize = Utility.ToEICFormat(befSpeed, "/s");
                    }
                });

                var parallelSucc = Parallel.ForEach(
                    this.m_images,
                    (e, state) =>
                    {
                        for (var index = 0; index < e.ImageUri.Length; ++index)
                        {
                            var succ = Utility.Retry(() => this.DownloadWorker(e, index));

                            if (succ)
                                return;
                        }

                        state.Stop();
                    }).IsCompleted;

                stopSlim.Set();
                updateTask.Wait();

                if (!parallelSucc)
                    return false;
            }

            this.SpeedOrFileSize = null;

            // 모든 이미지가 다운로드가 완료되어야 함
            return this.m_images.All(e => e.Extension != null);
        }

        private bool DownloadWorker(ImageInfomation e, int uriIndex)
        {
            var req = WebClientEx.AddHeader(WebRequest.Create(e.ImageUri[uriIndex]));
            if (req is HttpWebRequest hreq)
            {
                hreq.Referer = this.Uri.AbsoluteUri;
                hreq.AllowWriteStreamBuffering = false;
                hreq.AllowReadStreamBuffering = false;
            }

            using (var res = req.GetResponse() as HttpWebResponse)
            using (var resBody = res.GetResponseStream())
            {
                e.TempStream.SetLength(0);

                var buff = new byte[App.BufferSize];
                int read;

                while ((read = resBody.Read(buff, 0, App.BufferSize)) > 0)
                {
                    Interlocked.Add(ref this.m_downloaded, read);
                    e.TempStream.Write(buff, 0, read);
                }

                e.TempStream.Flush();

                e.TempStream.Position = 0;
                e.Extension = Signatures.GetExtension(e.TempStream);
            }

            // 이미지 암호화 푸는 작업
            this.m_decryptor.Decrypt(e.TempStream);

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

                zipStream.SetComment(this.Uri.AbsoluteUri + "\nby DaruDaru");
                zipStream.SetLevel(0);

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

        private class Assets
        {
            [JsonProperty("message")]
            public string Message { get; set; }

            [JsonProperty("sources")]
            public string[] Sources { get; set; }

            [JsonProperty("status")]
            public string Status { get; set; }
        }
    }
}
