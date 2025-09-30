using System;
using System.Net.Http;

namespace vcrutils
{
    public static class NotionHelper
    {
        // Global constants
        public const string NOTION_INVESTOR_RESEARCH_DATABASE_ID = "27b6ef03-8cf6-8059-9860-c0ec6873c896";

        // Singleton HTTP Client
        private static HttpClient? _notionClient;

        // HTTP Client Helper Method (Singleton)
        public static HttpClient GetNotionClient()
        {
            if (_notionClient == null)
            {
                string notionToken = Environment.GetEnvironmentVariable("NOTION_API_KEY");
                if (string.IsNullOrEmpty(notionToken))
                {
                    throw new InvalidOperationException("NOTION_API_KEY environment variable not set");
                }

                _notionClient = new HttpClient();
                _notionClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {notionToken}");
                _notionClient.DefaultRequestHeaders.Add("Notion-Version", "2022-06-28");
            }
            return _notionClient;
        }
    }
}
