using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

namespace Trucks
{
    class ExcelConverter
    {
        const string endpoint = "https://sandbox.zamzar.com";
        const string targetFormat = "xlsx";
        
        private readonly string key;

        public ExcelConverter(string apiKey)
        {
            this.key = apiKey; 
        }   
        
        public async Task<ZamzarResult> UploadAsync(string sourceFile)
        {
            const string url = endpoint + "/v1/jobs";

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

        public async Task<ZamzarResult> QueryAsync(int jobId)
        {
            string url = endpoint + "/v1/jobs/" + jobId.ToString();

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

        public async Task<IEnumerable<ZamzarResult>> QueryAllAsync()
        {
            string url = endpoint + "/v1/jobs/";
            List<ZamzarResult> results = null;

            using (HttpClientHandler handler = new HttpClientHandler { Credentials = new NetworkCredential(key, "")})
            using (HttpClient client = new HttpClient(handler))
            using (HttpResponseMessage response = await client.GetAsync(url))
            using (HttpContent content = response.Content)
            {
                string data = await content.ReadAsStringAsync();
                JsonDocument doc = JsonDocument.Parse(data);
                if (doc != null)
                {
                    var jobs = doc.RootElement.GetProperty("data");
                    results = JsonSerializer.Deserialize<List<ZamzarResult>>(jobs.GetRawText());
                }
            }
            
            // Get only the succesful ones.
            var successfulResults = results.Where(r => r.status == "successful");
            return successfulResults;
        }

        public async Task<bool> DownloadAsync(int fileId, string outputFile)
        {
            string url = endpoint + "/v1/files/" + fileId.ToString() + "/content";

            using (HttpClientHandler handler = new HttpClientHandler { Credentials = new NetworkCredential(key, "") })
            using (HttpClient client = new HttpClient(handler))
            using (HttpResponseMessage response = await client.GetAsync(url))
            using (HttpContent content = response.Content)
            using (Stream stream = await content.ReadAsStreamAsync())
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    string errors = GetErrors(stream);
                    if (errors != null)
                        System.Console.WriteLine($"Error downloading {outputFile}\n\t:{errors}");
                    return false;
                }
                else
                {
                    using (FileStream writer = File.Create(outputFile))
                    {
                        stream.CopyTo(writer);
                        return true;
                    }
                }
            }
        }

        private string GetErrors(Stream stream)     
        {
            try
            {
                JsonDocument doc = JsonDocument.Parse(stream);
                JsonElement errorsElement;
                if (doc.RootElement.TryGetProperty("errors", out errorsElement))
                    return errorsElement.GetRawText();
                else
                    return null;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }
    }

    public class ZamzarResult
    {
        public int id { get; set; }
        public string status { get; set; }

        public TargetFiles[] target_files { get; set; }        

        public override string ToString()
        {
            return $"{id}:{status}";
        }
    }

    public class TargetFiles
    {
        public int id { get; set; }
        public string name { get; set; }
    }
}