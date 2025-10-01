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
                            Console.WriteLine("   1. The database exists in the sagittal Notion workspace");
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

        public static async Task TestAttioList()
        {
            Console.WriteLine("📝 Testing Attio database lookup for both target lists...");

            try
            {
                HttpClient client = AttioHelper.GetAttioClient();

                try
                {
                    // First, list all available lists to find both target lists
                    Console.WriteLine("Fetching all lists...");
                    HttpResponseMessage response = await client.GetAsync("https://api.attio.com/v2/lists");
                    string responseBody = await response.Content.ReadAsStringAsync();

                    Console.WriteLine($"Status: {response.StatusCode}");

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("✅ Successfully fetched lists!");
                        JsonNode node = JsonNode.Parse(responseBody);
                        var lists = node?["data"]?.AsArray();

                        if (lists != null && lists.Count > 0)
                        {
                            Console.WriteLine($"Found {lists.Count} list(s):");

                            string? preseedVCsListId = null;
                            string? startupFundraisingListId = null;

                            foreach (var list in lists)
                            {
                                string name = list?["name"]?.ToString() ?? "Unknown";
                                string apiSlug = list?["api_slug"]?.ToString() ?? "unknown";
                                string listId = list?["id"]?["list_id"]?.ToString() ?? "unknown";

                                Console.WriteLine($"  - {name} (slug: {apiSlug}, id: {listId})");

                                if (name.Contains("Preseed VCs from Notion", StringComparison.OrdinalIgnoreCase))
                                {
                                    preseedVCsListId = listId;
                                    Console.WriteLine($"    ✅ Found target 'Preseed VCs from Notion' list!");
                                }
                                else if (name.Contains("Startup Fundraising", StringComparison.OrdinalIgnoreCase))
                                {
                                    startupFundraisingListId = listId;
                                    Console.WriteLine($"    ✅ Found target 'Startup Fundraising' list!");
                                }
                            }

                            // Test both lists if found
                            if (preseedVCsListId != null)
                            {
                                Console.WriteLine($"\n🔎 Getting details for Preseed VCs list (ID: {preseedVCsListId})...");

                                HttpResponseMessage listResponse = await client.GetAsync($"https://api.attio.com/v2/lists/{preseedVCsListId}");
                                string listResponseBody = await listResponse.Content.ReadAsStringAsync();

                                if (listResponse.IsSuccessStatusCode)
                                {
                                    Console.WriteLine("✅ Successfully retrieved Preseed VCs list details!");
                                    Console.WriteLine($"Preseed VCs list details: {listResponseBody}");
                                }
                                else
                                {
                                    Console.WriteLine($"❌ Failed to get Preseed VCs list details: {listResponse.StatusCode}");
                                    Console.WriteLine($"Response: {listResponseBody}");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"❌ 'Preseed VCs from Notion' list not found.");
                            }

                            if (startupFundraisingListId != null)
                            {
                                Console.WriteLine($"\n🔎 Getting details for Startup Fundraising list (ID: {startupFundraisingListId})...");

                                HttpResponseMessage listResponse = await client.GetAsync($"https://api.attio.com/v2/lists/{startupFundraisingListId}");
                                string listResponseBody = await listResponse.Content.ReadAsStringAsync();

                                if (listResponse.IsSuccessStatusCode)
                                {
                                    Console.WriteLine("✅ Successfully retrieved Startup Fundraising list details!");
                                    Console.WriteLine($"Startup Fundraising list details: {listResponseBody}");
                                }
                                else
                                {
                                    Console.WriteLine($"❌ Failed to get Startup Fundraising list details: {listResponse.StatusCode}");
                                    Console.WriteLine($"Response: {listResponseBody}");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"❌ 'Startup Fundraising' list not found.");
                            }

                            // Show summary
                            if (preseedVCsListId == null && startupFundraisingListId == null)
                            {
                                Console.WriteLine($"\n❌ Neither target list found. Available lists:");
                                foreach (var list in lists)
                                {
                                    string name = list?["name"]?.ToString() ?? "Unknown";
                                    Console.WriteLine($"   - {name}");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"\n📊 Summary:");
                                Console.WriteLine($"  - Preseed VCs from Notion: {(preseedVCsListId != null ? "✅ Found" : "❌ Not found")}");
                                Console.WriteLine($"  - Startup Fundraising: {(startupFundraisingListId != null ? "✅ Found" : "❌ Not found")}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("❌ No lists found");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"❌ Failed to fetch lists: {response.StatusCode}");
                        Console.WriteLine($"Response: {responseBody}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Attio list test failed: {ex.Message}");
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
