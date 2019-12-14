using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Trucks
{
    public class PantherClient
    {
        private const string patherBaseUrl = "http://fleetweb.pantherpremium.com";
        private string sessionId;
        private DateTime sessionExpires;

        public async Task<bool> LoginAsync(string companyId, string password)
        {
            bool isLoggedIn = false;
            
            string loginUrl = patherBaseUrl + "/Login/Login";
            string content = $"UserID={companyId}&Password={password}&RememberMe=false";

            var clientHandler = new HttpClientHandler();
            clientHandler.CookieContainer = new CookieContainer();            
            HttpClient client = new HttpClient(clientHandler);
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
            // IEnumerable<string> cookies; 
            // response.Headers.TryGetValues("Set-Cookie", out cookies);

            // foreach(var cookie in cookies)
            // {
            //     System.Console.WriteLine(cookie);
            // }

            return isLoggedIn;
        }
    }
}