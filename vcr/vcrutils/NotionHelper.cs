using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Markdig;

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

        /// <summary>
        /// Appends markdown content to a Notion page using the Mark2Notion API
        /// </summary>
        /// <param name="pageId">The Notion page ID to append content to</param>
        /// <param name="markdownContent">The markdown content to append</param>
        /// <param name="notionToken">The Notion API token</param>
        /// <returns>True if successful, false otherwise</returns>
        public static async Task<bool> AppendMarkdownToNotionPage(string pageId, string markdownContent, string notionToken)
        {
            try
            {
                using (HttpClient mark2NotionClient = new HttpClient())
                {
                    // Get Mark2Notion API key from environment variable
                    string mark2NotionApiKey = Environment.GetEnvironmentVariable("MARK2NOTION_API_KEY");
                    if (string.IsNullOrEmpty(mark2NotionApiKey))
                    {
                        Console.WriteLine("MARK2NOTION_API_KEY environment variable not set");
                        return false;
                    }

                    // Convert markdown to HTML using Markdig
                    string htmlContent = Markdown.ToHtml(markdownContent);

                    // Set up the Mark2Notion API request
                    mark2NotionClient.DefaultRequestHeaders.Add("x-api-key", mark2NotionApiKey);

                    var mark2NotionRequestBody = new
                    {
                        markdown = htmlContent,
                        notionToken = notionToken,
                        pageId = pageId
                    };

                    string mark2NotionJson = JsonSerializer.Serialize(mark2NotionRequestBody);
                    var mark2NotionContent = new StringContent(mark2NotionJson, Encoding.UTF8, "application/json");

                    // Call the Mark2Notion append API
                    HttpResponseMessage mark2NotionResponse = await mark2NotionClient.PostAsync("https://api.mark2notion.com/api/append", mark2NotionContent);
                    string mark2NotionResponseBody = await mark2NotionResponse.Content.ReadAsStringAsync();

                    if (mark2NotionResponse.IsSuccessStatusCode)
                    {
                        // Parse the Mark2Notion response
                        JsonNode mark2NotionNode = JsonNode.Parse(mark2NotionResponseBody);
                        string status = mark2NotionNode?["status"]?.ToString() ?? "unknown";

                        if (status == "success")
                        {
                            return true;
                        }
                        else
                        {
                            Console.WriteLine($"Mark2Notion API returned status: {status}");
                            return false;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Mark2Notion API error: {mark2NotionResponse.StatusCode}");
                        Console.WriteLine($"Response: {mark2NotionResponseBody}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error appending markdown to Notion page: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Creates a new Notion investor entry with domain, name, and markdown content
        /// </summary>
        /// <param name="domain">The investor domain</param>
        /// <param name="name">The investor name</param>
        /// <param name="markdownContent">The markdown content for the page</param>
        /// <returns>The page ID if successful, null otherwise</returns>
        public static async Task<string?> CreateNotionInvestorEntry(string domain, string name, string markdownContent)
        {
            string databaseId = NOTION_INVESTOR_RESEARCH_DATABASE_ID;

            try
            {
                HttpClient client = GetNotionClient();
                string notionToken = Environment.GetEnvironmentVariable("NOTION_API_KEY");

                if (string.IsNullOrEmpty(notionToken))
                {
                    Console.WriteLine("NOTION_API_KEY environment variable not set");
                    return null;
                }

                try
                {
                    // Step 1: Create database entry with basic fields
                    var createPageBody = new
                    {
                        parent = new { database_id = databaseId },
                        properties = new Dictionary<string, object>
                        {
                            ["Domain"] = new
                            {
                                url = domain.StartsWith("http") ? domain : $"https://{domain}"
                            },
                            ["Investor Name"] = new
                            {
                                title = new object[]
                                {
                                    new
                                    {
                                        type = "text",
                                        text = new { content = name }
                                    }
                                }
                            }
                        }
                    };

                    string createJson = JsonSerializer.Serialize(createPageBody);
                    var createContent = new StringContent(createJson, Encoding.UTF8, "application/json");

                    HttpResponseMessage createResponse = await client.PostAsync("https://api.notion.com/v1/pages", createContent);
                    string createResponseBody = await createResponse.Content.ReadAsStringAsync();

                    if (createResponse.IsSuccessStatusCode)
                    {
                        // Parse response to get the page ID
                        JsonNode createNode = JsonNode.Parse(createResponseBody);
                        string? pageId = createNode?["id"]?.ToString();

                        if (!string.IsNullOrEmpty(pageId))
                        {
                            // Step 2: Add markdown content to the page using Mark2Notion API
                            bool success = await AppendMarkdownToNotionPage(pageId, markdownContent, notionToken);
                            if (success)
                            {
                                return pageId;
                            }
                        }
                    }
                    return null; // Failed
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in CreateNotionInvestorEntry: {ex.Message}");
                    return null; // Failed
                }
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"❌ {ex.Message}");
                return null;
            }
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
