using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text.Json;

namespace Trucks
{
    class ExcelConverter
    {
        const string endpoint = "https://sandbox.zamzar.com/v1/jobs";
        const string targetFormat = "xlsx";
        private readonly string apiKey;

        public ExcelConverter(string apiKey)
        {
            this.apiKey = apiKey; 
        }   
        // "977abf9c5a3497234a089dadf19e329a3160ae1d"
        // var json = Upload(apiKey, endpoint, sourceFile, targetFormat).Result;
        // Console.WriteLine(json);
        
        public async Task<ZamzarResult> Upload(string key, string url, string sourceFile, string targetFormat)
        {
            using (HttpClientHandler handler = new HttpClientHandler { Credentials = new NetworkCredential(key, "") })
            using (HttpClient client = new HttpClient(handler))
            {
                var request = new MultipartFormDataContent();
                request.Add(new StringContent(targetFormat), "target_format");
                request.Add(new StreamContent(File.OpenRead(sourceFile)), "source_file", new FileInfo(sourceFile).Name);
                using (HttpResponseMessage response = await client.PostAsync(url, request).ConfigureAwait(false))
                using (HttpContent content = response.Content)
                {
                    string data = await content.ReadAsStringAsync();
                    ZamzarResult zResult = JsonSerializer.Deserialize<ZamzarResult>(data);
                    return zResult;
                }
            }
        }

        public async Task<ZamzarResult> Query(string key, string url)
        {
            using (HttpClientHandler handler = new HttpClientHandler { Credentials = new NetworkCredential(key, "")})
            using (HttpClient client = new HttpClient(handler))
            using (HttpResponseMessage response = await client.GetAsync(url))
            using (HttpContent content = response.Content)
            {
                string data = await content.ReadAsStringAsync();
                ZamzarResult zResult = JsonSerializer.Deserialize<ZamzarResult>(data);
                return zResult;
            }
        }   

        public async Task Download(string key, string url, string file)
        {
            using (HttpClientHandler handler = new HttpClientHandler { Credentials = new NetworkCredential(key, "") })
            using (HttpClient client = new HttpClient(handler))
            using (HttpResponseMessage response = await client.GetAsync(url))
            using (HttpContent content = response.Content)
            using (Stream stream = await content.ReadAsStreamAsync())
            using (FileStream writer = File.Create(file))
            {
                stream.CopyTo(writer);
            }
        }             
    }

    public class ZamzarResult
    {
        public int id { get; set; }
        public string status { get; set; }

        public override string ToString()
        {
            return $"{id}:{status}";
        }
    }
}