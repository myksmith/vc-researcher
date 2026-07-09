using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using vcrutils;

namespace VCR
{
    public static class TestCommands
    {
        public static async Task TestNotionConnection()
        {
            Console.WriteLine("🧪 Testing Notion API connection...");

            try
            {
                HttpClient client = NotionHelper.GetNotionClient();

                try
                {
                    // Test by searching for the 'Investor Research' database specifically
                    var searchBody = new
                    {
                        query = "Investor Research",
                        filter = new { property = "object", value = "database" }
                    };
                    string searchJson = JsonSerializer.Serialize(searchBody);
                    var searchContent = new StringContent(searchJson, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync("https://api.notion.com/v1/search", searchContent);
                    string responseBody = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("✅ Notion API connection successful!");
                        Console.WriteLine($"Response: {responseBody}");

                        // Parse to show available databases
                        JsonNode node = JsonNode.Parse(responseBody);
                        var results = node?["results"]?.AsArray();
                        if (results != null && results.Count > 0)
                        {
                            Console.WriteLine($"Found {results.Count} matching database(s):");
                            bool foundInvestorResearch = false;
                            foreach (var db in results)
                            {
                                string id = db?["id"]?.ToString() ?? "unknown";
                                string title = db?["title"]?.AsArray()?[0]?["plain_text"]?.ToString() ?? "Untitled";
                                Console.WriteLine($"  - {title} (ID: {id})");
                                if (title.Contains("Investor Research", StringComparison.OrdinalIgnoreCase))
                                {
                                    foundInvestorResearch = true;
                                    Console.WriteLine($"    ✅ Found target 'Investor Research' database!");
                                }
                            }
                            if (!foundInvestorResearch)
                            {
                                Console.WriteLine($"    ⚠️  'Investor Research' database not found in results");
                            }
                        }
                        else
                        {
                            Console.WriteLine("❌ No 'Investor Research' database found. Make sure:");
                            Console.WriteLine("   1. The database exists in your Notion workspace");
                            Console.WriteLine("   2. Your Notion integration has access to it");
                            Console.WriteLine("   3. The database is named 'Investor Research'");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"❌ Notion API error: {response.StatusCode}");
                        Console.WriteLine($"Response: {responseBody}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Notion API connection failed: {ex.Message}");
                }
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"❌ {ex.Message}");
            }
        }

        public static async Task PingAttio()
        {
            Console.WriteLine("🏓 Pinging Attio API...");

            try
            {
                HttpClient client = AttioHelper.GetAttioClient();

                try
                {
                    // Basic ping to list objects
                    HttpResponseMessage response = await client.GetAsync("https://api.attio.com/v2/objects");
                    string responseBody = await response.Content.ReadAsStringAsync();

                    Console.WriteLine($"Status: {response.StatusCode}");
                    Console.WriteLine($"Response: {responseBody}");

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("✅ Attio API ping successful!");
                    }
                    else
                    {
                        Console.WriteLine($"❌ Attio API ping failed with status: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Attio API ping failed: {ex.Message}");
                }
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"❌ {ex.Message}");
            }
        }

        public static async Task TestNotionInsert()
        {
            Console.WriteLine("📝 Testing Notion database entry creation with markdown content...");

            string testDomain = "testvc.vc";
            string testName = "TestVC";
            string testMarkdown = @"# TestVC Analysis

This is a test entry for TestVC (testvc.vc).

## Investment Criteria Match

- Stage: Seed stage focus
- Check size: $1M-$5M range

**Overall: Good test case for API integration.**";

            Console.WriteLine($"Creating entry for {testName} ({testDomain})...");
            string? pageId = await NotionHelper.CreateNotionInvestorEntry(testDomain, testName, testMarkdown);

            if (pageId != null)
            {
                Console.WriteLine("✅ TestVC entry created successfully!");
                Console.WriteLine($"Page ID: {pageId}");
                Console.WriteLine($"View at: https://notion.so/{pageId.Replace("-", "")}");
            }
            else
            {
                Console.WriteLine("❌ Failed to create TestVC entry");
            }
        }
    }
}
