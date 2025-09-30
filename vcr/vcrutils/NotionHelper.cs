using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

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

        public static async Task<bool> CheckNotionDomainExists(string investorDomain)
        {
            try
            {
                HttpClient client = GetNotionClient();

                try
                {
                    // Search the Investor Research database for pages with matching domain
                    var searchBody = new
                    {
                        filter = new
                        {
                            property = "Domain",
                            url = new
                            {
                                contains = investorDomain
                            }
                        }
                    };

                    string searchJson = JsonSerializer.Serialize(searchBody);
                    var searchContent = new StringContent(searchJson, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAsync($"https://api.notion.com/v1/databases/{NOTION_INVESTOR_RESEARCH_DATABASE_ID}/query", searchContent);
                    string responseBody = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        JsonNode node = JsonNode.Parse(responseBody);
                        var results = node?["results"]?.AsArray();

                        return results != null && results.Count > 0;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error checking Notion database: {ex.Message}");
                    return false;
                }
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"❌ {ex.Message}");
                return false;
            }
        }

        public static async Task<string?> FindExistingNotionPageId(string investorDomain)
        {
            try
            {
                HttpClient client = GetNotionClient();

                try
                {
                    // Search the Investor Research database for pages with matching domain
                    var searchBody = new
                    {
                        filter = new
                        {
                            property = "Domain",
                            url = new
                            {
                                contains = investorDomain
                            }
                        }
                    };

                    string searchJson = JsonSerializer.Serialize(searchBody);
                    var searchContent = new StringContent(searchJson, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAsync($"https://api.notion.com/v1/databases/{NOTION_INVESTOR_RESEARCH_DATABASE_ID}/query", searchContent);
                    string responseBody = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        JsonNode node = JsonNode.Parse(responseBody);
                        var results = node?["results"]?.AsArray();

                        if (results != null && results.Count > 0)
                        {
                            // Get the first matching page
                            var firstResult = results[0];
                            string? pageId = firstResult?["id"]?.ToString();

                            if (!string.IsNullOrEmpty(pageId))
                            {
                                return pageId;
                            }
                        }
                    }

                    return null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error searching Notion database: {ex.Message}");
                    return null;
                }
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"❌ {ex.Message}");
                return null;
            }
        }

        public static async Task<bool> DeleteNotionPage(string pageId)
        {
            try
            {
                HttpClient client = GetNotionClient();

                try
                {
                    Console.WriteLine($"🗑️  Deleting Notion page (ID: {pageId})...");

                    // Archive (delete) the page using Notion API
                    var updateBody = new
                    {
                        archived = true
                    };

                    string updateJson = JsonSerializer.Serialize(updateBody);
                    var updateContent = new StringContent(updateJson, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PatchAsync($"https://api.notion.com/v1/pages/{pageId}", updateContent);
                    string responseBody = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("✅ Successfully deleted existing Notion page");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"❌ Failed to delete Notion page: {response.StatusCode}");
                        Console.WriteLine($"Response: {responseBody}");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error deleting Notion page: {ex.Message}");
                    return false;
                }
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"❌ {ex.Message}");
                return false;
            }
        }
    }
}
