using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Trucks
{
    public class PantherClient
    {
        private const string pantherBaseUrl = "http://fleetweb.pantherpremium.com";
        private string sessionId;
        private DateTime sessionExpires;
        HttpClientHandler clientHandler;
        HttpClient client;

        public PantherClient()
        {
            clientHandler = new HttpClientHandler();
            clientHandler.CookieContainer = new CookieContainer();            
            client = new HttpClient(clientHandler);
        }

        public async Task<bool> LoginAsync(string companyId, string password)
        {
            bool isLoggedIn = false;
            
            string loginUrl = pantherBaseUrl + "/Login/Login";
            string content = $"UserID={companyId}&Password={password}&RememberMe=false";
            StringContent httpContent = new StringContent(content);
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            
            var response = await client.PostAsync(loginUrl, httpContent);
            response.EnsureSuccessStatusCode();

            var contents = await response.Content.ReadAsStringAsync();
            CookieCollection cookieCollection = clientHandler.CookieContainer.GetCookies(new Uri(loginUrl));
            foreach (Cookie cookie in cookieCollection)
            {
                if (cookie.Name == "session-id")
                {
                    sessionId = cookie.Value;
                    sessionExpires = cookie.Expires;
                    isLoggedIn = true;
                    break;
                }
            }
            return isLoggedIn;
        }
    
        public Task<string> GetPayrollHistAsync()
        {
            string uri = pantherBaseUrl + "/Financial/PayrollHist";
            return client.GetStringAsync(uri);
        }

        /// <summary>
        /// Downloads an Excel file to disk and returns the path of the saved file.
        /// </summary>
        public async Task<string> DownloadSettlementReportAsync(string checkNumber)
        {
            string uri = pantherBaseUrl + $"/Financial/DownloadSettlementReport?ChkNo={checkNumber}";
            byte[] bytes = await client.GetByteArrayAsync(uri);
            string filename = $"{checkNumber}.xls";
            File.WriteAllBytes(filename, bytes);
            return filename;
        }
    }
}