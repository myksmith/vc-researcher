using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Nodes;
using System.Text.Json;

class Program
{
    static async Task Main(string[] args)
    {
        // Argument validation
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: dotnet run <investor-domain>");
            Console.WriteLine("Example: dotnet run example-vc.com");
            Console.WriteLine("\nTest commands:");
            Console.WriteLine("  dotnet run --test-notion      # Test Notion API connection");
            Console.WriteLine("  dotnet run --test-notion-insert # Test Notion database entry creation with markdown");
            Console.WriteLine("  dotnet run --ping-attio       # Ping Attio API for basic connectivity");
            Console.WriteLine("  dotnet run --test-attio-list  # Test Attio list lookup for 'Preseed VCs from Notion'");
            return;
        }

        // Handle test commands
        if (args[0] == "--test-notion")
        {
            await TestNotionConnection();
            return;
        }
        
        if (args[0] == "--test-notion-insert")
        {
            await TestNotionInsert();
            return;
        }
        
        if (args[0] == "--ping-attio")
        {
            await PingAttio();
            return;
        }
        
        if (args[0] == "--test-attio-list")
        {
            await TestAttioList();
            return;
        }

        string investorDomain = args[0];

        try
        {
            // Step 1: Verify records exist in both systems BEFORE doing expensive Perplexity call
            Console.WriteLine($"🔍 Looking up records for {investorDomain}...");

            string notionRecordId = await FindNotionRecord(investorDomain);
            string attioRecordId = await FindAttioRecord(investorDomain);

            // Early exit if either record is missing
            if (notionRecordId == null)
            {
                Console.WriteLine($"❌ Could not find Notion record for {investorDomain}");
                return;
            }

            if (attioRecordId == null)
            {
                Console.WriteLine($"❌ Could not find Attio record for {investorDomain}");
                return;
            }

            Console.WriteLine("✅ Found records in both Notion and Attio");

            // Step 2: Get analysis from Perplexity (only after confirming records exist)
            string analysis = await QueryPerplexityForVCAnalysis(investorDomain);
            Console.WriteLine("✅ Completed Perplexity analysis");

            // Step 3: Update Notion database
            await UpdateNotionDatabase(notionRecordId, investorDomain, analysis);
            Console.WriteLine("✅ Updated Notion database");

            // Step 4: Update Attio CRM record
            await UpdateAttioCRM(attioRecordId, investorDomain, analysis);
            Console.WriteLine("✅ Updated Attio CRM");

            Console.WriteLine($"🎉 Successfully processed {investorDomain}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error processing {investorDomain}: {ex.Message}");
        }
    }

    static async Task<string> QueryPerplexityForVCAnalysis(string investorDomain)
    {
        string apiUrl = "https://api.perplexity.ai/chat/completions";
        string apiKey = Environment.GetEnvironmentVariable("SONAR_API_KEY");

        // Read the investor criteria file
        string criteriaFilePath = "Neo_Investor_Search_Criteria.md";
        string investorCriteria = "";

        try
        {
            if (File.Exists(criteriaFilePath))
            {
                investorCriteria = await File.ReadAllTextAsync(criteriaFilePath);
                Console.WriteLine($"Loaded investor criteria from {criteriaFilePath}");
            }
            else
            {
                Console.WriteLine($"Warning: {criteriaFilePath} not found. Proceeding without specific criteria.");
                investorCriteria = "general venture capital investment criteria";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading criteria file: {ex.Message}");
            investorCriteria = "general venture capital investment criteria";
        }

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var requestBody = new
            {
                model = "sonar-pro",
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = $"Research the venture capital firm at {investorDomain} and evaluate whether it would be a good fit for Neo's $5M seed round based on the specific investor criteria provided below.\n\n" +
                                 $"INVESTOR CRITERIA CONTEXT:\n{investorCriteria}\n\n" +
                                 $"Please analyze {investorDomain} against these criteria and provide:\n" +
                                 $"1. How well they match our stage, check size, and sector focus\n" +
                                 $"2. Their relevant portfolio companies and track record\n" +
                                 $"3. Geographic alignment and investment thesis fit\n" +
                                 $"4. Overall recommendation (Strong Fit / Good Fit / Weak Fit / No Fit)\n" +
                                 $"5. Any specific partners or team members to target\n" +
                                 $"6. Potential concerns or red flags"
                    }
                }
            };

            string jsonBody = System.Text.Json.JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = await client.PostAsync(apiUrl, content);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Response: " + responseBody);

                JsonNode node = JsonNode.Parse(responseBody);
                string chatResponse = node["choices"][0]["message"]["content"].ToString();

                Console.WriteLine("Chat response only: " + chatResponse);
                return chatResponse;
            }
            catch (Exception ex)
            {
                throw new Exception($"Perplexity API error: {ex.Message}");
            }
        }
    }

    static async Task<string> FindNotionRecord(string investorDomain)
    {
        // TODO: Search Notion database for matching record
        // Return record ID if found, null if not found
        await Task.Delay(100); // Simulate API call
        
        // Stubbed: Always return a mock record ID for now
        return $"notion-record-{investorDomain.Replace(".", "-")}";
    }

    static async Task<string> FindAttioRecord(string investorDomain)
    {
        // First, find the Preseed VCs list
        string? listId = await FindAttioPreseedVCsList();
        if (listId == null)
        {
            return null;
        }
        
        // TODO: Search within the list for records matching the domain
        // For now, return the list ID as a placeholder
        await Task.Delay(100); // Simulate additional lookup logic
        
        // Stubbed: Return list ID for now (will need to search within list entries)
        return listId;
    }

    static async Task UpdateNotionDatabase(string recordId, string investorDomain, string analysis)
    {
        // TODO: Implement Notion API integration
        await Task.Delay(100); // Simulate API call
        Console.WriteLine($"[STUB] Would update Notion record {recordId} with analysis for {investorDomain}");
    }

    static async Task UpdateAttioCRM(string recordId, string investorDomain, string analysis)
    {
        // TODO: Implement Attio API integration
        await Task.Delay(100); // Simulate API call
        Console.WriteLine($"[STUB] Would update Attio record {recordId} with analysis for {investorDomain}");
    }

    // Test functions for API connectivity
    static async Task TestNotionConnection()
    {
        Console.WriteLine("🧪 Testing Notion API connection...");
        
        string notionToken = Environment.GetEnvironmentVariable("NOTION_API_KEY");
        if (string.IsNullOrEmpty(notionToken))
        {
            Console.WriteLine("❌ NOTION_API_KEY environment variable not set");
            return;
        }

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {notionToken}");
            client.DefaultRequestHeaders.Add("Notion-Version", "2022-06-28");

            try
            {
                // Test by searching for the 'Investor Research' database specifically
                var searchBody = new 
                {
                    query = "Investor Research",
                    filter = new { property = "object", value = "database" }
                };
                string searchJson = System.Text.Json.JsonSerializer.Serialize(searchBody);
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
    }

    static async Task PingAttio()
    {
        Console.WriteLine("🏓 Pinging Attio API...");
        
        string attioToken = Environment.GetEnvironmentVariable("ATTIO_API_KEY");
        if (string.IsNullOrEmpty(attioToken))
        {
            Console.WriteLine("❌ ATTIO_API_KEY environment variable not set");
            return;
        }

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {attioToken}");

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
    }

    static async Task<string?> FindAttioPreseedVCsList()
    {
        string attioToken = Environment.GetEnvironmentVariable("ATTIO_API_KEY");
        if (string.IsNullOrEmpty(attioToken))
        {
            return null;
        }

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {attioToken}");

            try
            {
                // List all available lists to find "Preseed VCs from Notion"
                HttpResponseMessage response = await client.GetAsync("https://api.attio.com/v2/lists");
                string responseBody = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    JsonNode node = JsonNode.Parse(responseBody);
                    var lists = node?["data"]?.AsArray();
                    
                    if (lists != null && lists.Count > 0)
                    {
                        foreach (var list in lists)
                        {
                            string name = list?["name"]?.ToString() ?? "Unknown";
                            string listId = list?["id"]?["list_id"]?.ToString() ?? "unknown";
                            
                            if (name.Contains("Preseed VCs from Notion", StringComparison.OrdinalIgnoreCase))
                            {
                                return listId;
                            }
                        }
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }

    static async Task TestAttioList()
    {
        Console.WriteLine("📝 Testing Attio 'Preseed VCs from Notion' list lookup...");
        
        string attioToken = Environment.GetEnvironmentVariable("ATTIO_API_KEY");
        if (string.IsNullOrEmpty(attioToken))
        {
            Console.WriteLine("❌ ATTIO_API_KEY environment variable not set");
            return;
        }

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {attioToken}");

            try
            {
                // First, list all available lists to find "Preseed VCs from Notion"
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
                        }
                        
                        if (preseedVCsListId != null)
                        {
                            Console.WriteLine($"\n🔎 Getting details for Preseed VCs list (ID: {preseedVCsListId})...");
                            
                            // Get the specific list details
                            HttpResponseMessage listResponse = await client.GetAsync($"https://api.attio.com/v2/lists/{preseedVCsListId}");
                            string listResponseBody = await listResponse.Content.ReadAsStringAsync();
                            
                            if (listResponse.IsSuccessStatusCode)
                            {
                                Console.WriteLine("✅ Successfully retrieved Preseed VCs list details!");
                                Console.WriteLine($"List details: {listResponseBody}");
                            }
                            else
                            {
                                Console.WriteLine($"❌ Failed to get list details: {listResponse.StatusCode}");
                                Console.WriteLine($"Response: {listResponseBody}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"❌ 'Preseed VCs from Notion' list not found. Available lists:");
                            foreach (var list in lists)
                            {
                                string name = list?["name"]?.ToString() ?? "Unknown";
                                Console.WriteLine($"   - {name}");
                            }
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
    }

    static async Task TestNotionInsert()
    {
        Console.WriteLine("📝 Testing Notion database entry creation with markdown content...");
        
        string notionToken = Environment.GetEnvironmentVariable("NOTION_API_KEY");
        if (string.IsNullOrEmpty(notionToken))
        {
            Console.WriteLine("❌ NOTION_API_KEY environment variable not set");
            return;
        }

        string databaseId = "27b6ef03-8cf6-8059-9860-c0ec6873c896"; // Investor Research database ID

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {notionToken}");
            client.DefaultRequestHeaders.Add("Notion-Version", "2022-06-28");

            try
            {
                // Step 1: Create database entry with basic fields
                Console.WriteLine("Creating database entry for testvc...");
                
                var createPageBody = new
                {
                    parent = new { database_id = databaseId },
                    properties = new Dictionary<string, object>
                    {
                        ["Domain"] = new
                        {
                            url = "https://testvc.vc"
                        },
                        ["Investor Name"] = new
                        {
                            title = new object[]
                            {
                                new
                                {
                                    type = "text",
                                    text = new { content = "TestVC" }
                                }
                            }
                        }
                    }
                };

                string createJson = System.Text.Json.JsonSerializer.Serialize(createPageBody);
                var createContent = new StringContent(createJson, Encoding.UTF8, "application/json");
                
                HttpResponseMessage createResponse = await client.PostAsync("https://api.notion.com/v1/pages", createContent);
                string createResponseBody = await createResponse.Content.ReadAsStringAsync();
                
                if (createResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine("✅ Database entry created successfully!");
                    
                    // Parse response to get the page ID
                    JsonNode createNode = JsonNode.Parse(createResponseBody);
                    string pageId = createNode?["id"]?.ToString();
                    
                    if (!string.IsNullOrEmpty(pageId))
                    {
                        Console.WriteLine($"Page ID: {pageId}");
                        
                        // Step 2: Add markdown content to the page
                        Console.WriteLine("Adding markdown content to page...");
                        
                        var markdownContent = new
                        {
                            children = new object[]
                            {
                                new
                                {
                                    type = "heading_1",
                                    heading_1 = new
                                    {
                                        rich_text = new object[]
                                        {
                                            new
                                            {
                                                type = "text",
                                                text = new { content = "TestVC Analysis" }
                                            }
                                        }
                                    }
                                },
                                new
                                {
                                    type = "paragraph",
                                    paragraph = new
                                    {
                                        rich_text = new object[]
                                        {
                                            new
                                            {
                                                type = "text",
                                                text = new { content = "This is a test entry for TestVC (testvc.vc)." }
                                            }
                                        }
                                    }
                                },
                                new
                                {
                                    type = "heading_2",
                                    heading_2 = new
                                    {
                                        rich_text = new object[]
                                        {
                                            new
                                            {
                                                type = "text",
                                                text = new { content = "Investment Criteria Match" }
                                            }
                                        }
                                    }
                                },
                                new
                                {
                                    type = "bulleted_list_item",
                                    bulleted_list_item = new
                                    {
                                        rich_text = new object[]
                                        {
                                            new
                                            {
                                                type = "text",
                                                text = new { content = "Stage: Seed stage focus" }
                                            }
                                        }
                                    }
                                },
                                new
                                {
                                    type = "bulleted_list_item",
                                    bulleted_list_item = new
                                    {
                                        rich_text = new object[]
                                        {
                                            new
                                            {
                                                type = "text",
                                                text = new { content = "Check size: $1M-$5M range" }
                                            }
                                        }
                                    }
                                },
                                new
                                {
                                    type = "paragraph",
                                    paragraph = new
                                    {
                                        rich_text = new object[]
                                        {
                                            new
                                            {
                                                type = "text",
                                                text = new { content = "Overall: Good test case for API integration." },
                                                annotations = new { bold = true }
                                            }
                                        }
                                    }
                                }
                            }
                        };
                        
                        string contentJson = System.Text.Json.JsonSerializer.Serialize(markdownContent);
                        var contentHttpContent = new StringContent(contentJson, Encoding.UTF8, "application/json");
                        
                        HttpResponseMessage contentResponse = await client.PatchAsync($"https://api.notion.com/v1/blocks/{pageId}/children", contentHttpContent);
                        string contentResponseBody = await contentResponse.Content.ReadAsStringAsync();
                        
                        if (contentResponse.IsSuccessStatusCode)
                        {
                            Console.WriteLine("✅ Markdown content added to page successfully!");
                            Console.WriteLine($"TestVC entry created with page content at: https://notion.so/{pageId.Replace("-", "")}");
                        }
                        else
                        {
                            Console.WriteLine($"❌ Failed to add content to page: {contentResponse.StatusCode}");
                            Console.WriteLine($"Response: {contentResponseBody}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("❌ Could not extract page ID from response");
                    }
                }
                else
                {
                    Console.WriteLine($"❌ Failed to create database entry: {createResponse.StatusCode}");
                    Console.WriteLine($"Response: {createResponseBody}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Notion insert test failed: {ex.Message}");
            }
        }
    }
}
