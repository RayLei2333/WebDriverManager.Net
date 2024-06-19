using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WebDriverManager.Helpers;
using WebDriverManager.Models.Chrome;

namespace WebDriverManager.Clients
{
    public static class ChromeForTestingClient
    {
        private static readonly string BaseUrl = "https://googlechromelabs.github.io/chrome-for-testing/";

        private static HttpClient _httpClient;

        private static HttpClient HttpClient
        {
            get
            {
                var handler = new HttpClientHandler
                {
                    UseProxy = Proxy != null,
                    Proxy = Proxy
                };

                _httpClient = new HttpClient(handler)
                {
                    BaseAddress = new Uri(BaseUrl)
                };

                _httpClient.DefaultRequestHeaders.Add("User-Agent", "WebDriverManager.NET");
                _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
                _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br, zstd");
                _httpClient.DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.9");
                return _httpClient;
            }
        }

        public static IWebProxy Proxy { get; set; }

        public static ChromeVersions GetKnownGoodVersionsWithDownloads()
        {
            return GetResultFromHttpTask<ChromeVersions>(
                HttpClient.GetAsync("known-good-versions-with-downloads.json")
            );
        }

        public static ChromeVersions GetLastKnownGoodVersions()
        {
            return GetResultFromHttpTask<ChromeVersions>(
                HttpClient.GetAsync("last-known-good-versions-with-downloads.json")
            );
        }

        /// <summary>
        /// Get a HTTP result without causing any deadlocks
        /// <para>See: https://learn.microsoft.com/en-us/archive/blogs/jpsanders/asp-net-do-not-use-task-result-in-main-context</para>
        /// </summary>
        /// <typeparam name="TResult">The type of result to convert the HTTP response to</typeparam>
        /// <param name="taskToRun">The <see cref="HttpResponseMessage"/> task to run</param>
        private static TResult GetResultFromHttpTask<TResult>(Task<HttpResponseMessage> taskToRun)
            where TResult : class
        {
            var httpTask = Task.Run(() => taskToRun);
            httpTask.Wait();
            var response = httpTask.Result;
            if (response.Content.Headers.Contains("Content-Encoding"))
            {
                string encoding = response.Content.Headers.GetValues("Content-Encoding").FirstOrDefault();
                if (string.Equals(encoding, "gzip", StringComparison.OrdinalIgnoreCase))
                {
                    var readBytesTask = Task.Run(() => httpTask.Result.Content.ReadAsByteArrayAsync());
                    readBytesTask.Wait();

                    byte[] decompressionData = GzipDecompression.DecompressGzip(readBytesTask.Result);
                    return JsonConvert.DeserializeObject<TResult>(Encoding.UTF8.GetString(decompressionData));
                }
            }


            var readStringTask = Task.Run(() => httpTask.Result.Content.ReadAsStringAsync());
            readStringTask.Wait();
            return JsonConvert.DeserializeObject<TResult>(readStringTask.Result);


        }
    }
}
