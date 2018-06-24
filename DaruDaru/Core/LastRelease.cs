using System;
using System.IO;
using System.Net;
using System.Reflection;
using Newtonsoft.Json;

namespace DaruDaru.Core
{
    internal class LastestRealease
    {
        [JsonProperty("tag_name")]
        public string TagName { get; set; }

        [JsonProperty("html_url")]
        public string HtmlUrl { get; set; }

        [JsonProperty("assets")]
        public Asset[] Assets { get; set; }

        [JsonObject]
        public class Asset
        {
            [JsonProperty("browser_download_url")]
            public string BrowserDownloadUrl { get; set; }
        }
    }

    internal static class LastRelease
    {
        public static LastestRealease CheckNewVersion()
        {
            try
            {
                LastestRealease last;

                var req = WebRequest.CreateHttp("https://api.github.com/repos/RyuaNerin/DaruDaru/releases/latest");
                req.Timeout = 5000;
                req.UserAgent = "QIT";
                using (var res = req.GetResponse())
                {
                    var json = new JsonSerializer();

                    using (var rStream = res.GetResponseStream())
                    using (var sReader = new StreamReader(rStream))
                    using (var jReader = new JsonTextReader(sReader))
                    {
                        last = json.Deserialize<LastestRealease>(jReader);
                    }
                }

                return new Version(last.TagName) > Assembly.GetExecutingAssembly().GetName().Version ? last : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
