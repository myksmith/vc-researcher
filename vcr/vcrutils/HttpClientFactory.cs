using System;
using System.Net.Http;

namespace vcrutils
{
    public static class HttpClientFactory
    {
        public static HttpClient CreateNotionClient()
        {
            string notionToken = Environment.GetEnvironmentVariable("NOTION_API_KEY");
            if (string.IsNullOrEmpty(notionToken))
            {
                throw new InvalidOperationException("NOTION_API_KEY environment variable not set");
            }

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {notionToken}");
            client.DefaultRequestHeaders.Add("Notion-Version", "2022-06-28");
            return client;
        }

        public static HttpClient CreateAttioClient()
        {
            string attioToken = Environment.GetEnvironmentVariable("ATTIO_API_KEY");
            if (string.IsNullOrEmpty(attioToken))
            {
                throw new InvalidOperationException("ATTIO_API_KEY environment variable not set");
            }

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {attioToken}");
            return client;
        }
    }
}
