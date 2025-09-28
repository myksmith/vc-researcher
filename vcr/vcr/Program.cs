using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Nodes;
using System.Text.Json;

class Program
{
    // Global constants
    private const string NOTION_INVESTOR_RESEARCH_DATABASE_ID = "27b6ef03-8cf6-8059-9860-c0ec6873c896";
    private const string ATTIO_STARTUP_FUNDRAISING_LIST_ID = "978d31e8-588c-443e-be6e-3023d9d2b750";
    private const string ATTIO_PRESEED_VCS_LIST_ID = "9420a9dd-773d-49b1-be02-e98077c29b94";
    static async Task Main(string[] args)
    {
        // Environment variable validation - check all required API keys
        var requiredEnvVars = new Dictionary<string, string>
        {
            { "SONAR_API_KEY", "Perplexity API" },
            { "NOTION_API_KEY", "Notion API" },
            { "ATTIO_API_KEY", "Attio CRM API" },
            { "MARK2NOTION_API_KEY", "Mark2Notion API" }
        };

        var missingVars = new List<string>();
        
        foreach (var envVar in requiredEnvVars)
        {
            string value = Environment.GetEnvironmentVariable(envVar.Key);
            if (string.IsNullOrEmpty(value))
            {
                missingVars.Add($"{envVar.Key} ({envVar.Value})");
            }
        }

        if (missingVars.Count > 0)
        {
            Console.WriteLine("❌ Missing required environment variables:");
            foreach (var missing in missingVars)
            {
                Console.WriteLine($"  - {missing}");
            }
            Console.WriteLine("\nPlease set all required API keys before running the application.");
            Console.WriteLine("Example:");
            Console.WriteLine("  export SONAR_API_KEY=\"your_perplexity_key\"");
            Console.WriteLine("  export NOTION_API_KEY=\"your_notion_key\"");
            Console.WriteLine("  export ATTIO_API_KEY=\"your_attio_key\"");
            Console.WriteLine("  export MARK2NOTION_API_KEY=\"your_mark2notion_key\"");
            Environment.Exit(1);
            return;
        }

        Console.WriteLine("✅ All required API keys are configured");

        // Argument validation
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: dotnet run <investor-domain>");
            Console.WriteLine("Example: dotnet run example-vc.com");
            Console.WriteLine("\nTest commands:");
            Console.WriteLine("  dotnet run --test-notion      # Test Notion API connection");
            Console.WriteLine("  dotnet run --test-notion-insert # Test Notion database entry creation with markdown");
            Console.WriteLine("  dotnet run --ping-attio       # Ping Attio API for basic connectivity");
            Console.WriteLine("  dotnet run --test-attio-list  # Test Attio list lookup for both target databases");
            Console.WriteLine("\nUtility commands:");
            Console.WriteLine("  dotnet run --fix-links <domain> # Update Attio with existing Notion research URL (no new research)");
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
        
        if (args[0] == "--fix-links")
        {
            if (args.Length < 2)
            {
                Console.WriteLine("❌ Usage: dotnet run --fix-links <investor-domain>");
                Console.WriteLine("Example: dotnet run --fix-links sequoiacap.com");
                return;
            }
            
            string domain = args[1];
            await FixAttioLinks(domain);
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
            string? notionPageUrl = await UpdateNotionDatabase(notionRecordId, investorDomain, analysis);
            if (notionPageUrl == null)
            {
                Console.WriteLine("❌ Failed to create Notion page - cannot update Attio with URL");
                return;
            }
            Console.WriteLine("✅ Updated Notion database");

            // Step 4: Update Attio CRM record with the Notion URL
            await UpdateAttioCRM(attioRecordId, investorDomain, notionPageUrl);
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
                                 $"IMPORTANT: Format your response as proper Markdown. Start your response with exactly this format on the first line:\n" +
                                 $"VC Name: [Full Name of the VC Firm]\n\n" +
                                 $"Then provide a comprehensive markdown analysis covering:\n" +
                                 $"1. How well they match our stage, check size, and sector focus\n" +
                                 $"2. Their relevant portfolio companies and track record\n" +
                                 $"3. Geographic alignment and investment thesis fit\n" +
                                 $"4. Overall recommendation (Strong Fit / Good Fit / Weak Fit / No Fit)\n" +
                                 $"5. Any specific partners or team members to target\n" +
                                 $"6. Potential concerns or red flags\n\n" +
                                 $"Use proper markdown formatting with headers, bullet points, bold text, etc."
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
        // Check both lists for the investor record
        var listIds = await FindAttioBothLists();
        if (listIds.preseedVCs == null && listIds.startupFundraising == null)
        {
            return null;
        }
        
        // TODO: Search within both lists for records matching the domain
        // For now, return the first available list ID as a placeholder
        await Task.Delay(100); // Simulate additional lookup logic
        
        // Stubbed: Return first available list ID for now (will need to search within list entries)
        return listIds.preseedVCs ?? listIds.startupFundraising;
    }

    static async Task<string?> UpdateNotionDatabase(string recordId, string investorDomain, string analysis)
    {
        // Extract VC name from the analysis response
        string vcName = ExtractVCNameFromResponse(analysis);
        
        // If extraction failed, fall back to domain-based name
        if (vcName == "Unknown VC")
        {
            vcName = investorDomain.Replace(".com", "").Replace(".vc", "").Replace(".", " ");
            vcName = char.ToUpper(vcName[0]) + vcName.Substring(1);
        }
        
        string? pageId = await CreateNotionInvestorEntry(investorDomain, vcName, analysis);
        
        if (pageId != null)
        {
            string notionUrl = $"https://notion.so/{pageId.Replace("-", "")}";
            Console.WriteLine($"Created Notion entry for {vcName}: {notionUrl}");
            return notionUrl;
        }
        else
        {
            Console.WriteLine($"Failed to create Notion entry for {investorDomain}");
            return null;
        }
    }

    static async Task UpdateAttioCRM(string recordId, string investorDomain, string notionUrl)
    {
        string attioToken = Environment.GetEnvironmentVariable("ATTIO_API_KEY");
        if (string.IsNullOrEmpty(attioToken))
        {
            Console.WriteLine("❌ ATTIO_API_KEY not set, cannot update Attio records");
            return;
        }

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {attioToken}");

            try
            {
                Console.WriteLine($"🔍 Searching for {investorDomain} in both Attio lists...");
                
                // Try to find and update the record in both lists
                bool updatedStartupFundraising = await UpdateAttioRecord(client, ATTIO_STARTUP_FUNDRAISING_LIST_ID, "Startup Fundraising", investorDomain, notionUrl);
                bool updatedPreseedVCs = await UpdateAttioRecord(client, ATTIO_PRESEED_VCS_LIST_ID, "Preseed VCs from Notion", investorDomain, notionUrl);
                
                if (updatedStartupFundraising || updatedPreseedVCs)
                {
                    Console.WriteLine($"✅ Successfully updated Notion Research URL for {investorDomain}");
                    if (updatedStartupFundraising) Console.WriteLine($"   - Updated in Startup Fundraising list");
                    if (updatedPreseedVCs) Console.WriteLine($"   - Updated in Preseed VCs from Notion list");
                }
                else
                {
                    Console.WriteLine($"⚠️  No records found for {investorDomain} in either list");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error updating Attio records: {ex.Message}");
            }
        }
    }
    
    static async Task<bool> UpdateAttioRecord(HttpClient client, string listId, string listName, string investorDomain, string notionUrl)
    {
        try
        {
            // TODO: Search within the list for records matching the domain
            // For now, this is stubbed as we need to implement the search logic
            Console.WriteLine($"🔍 Searching for {investorDomain} in {listName} list (ID: {listId})...");
            
            // Placeholder: In a real implementation, we would:
            // 1. Search list entries for records matching the domain
            // 2. Get the record ID 
            // 3. Update the "Notion Research URL" field with the notionUrl
            
            Console.WriteLine($"[STUB] Would update record in {listName} with URL: {notionUrl}");
            return false; // Return true when actual implementation is complete
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error updating {listName}: {ex.Message}");
            return false;
        }
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

    static async Task<(string? preseedVCs, string? startupFundraising)> FindAttioBothLists()
    {
        string attioToken = Environment.GetEnvironmentVariable("ATTIO_API_KEY");
        if (string.IsNullOrEmpty(attioToken))
        {
            return (null, null);
        }

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {attioToken}");

            try
            {
                // List all available lists to find both target lists
                HttpResponseMessage response = await client.GetAsync("https://api.attio.com/v2/lists");
                string responseBody = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    JsonNode node = JsonNode.Parse(responseBody);
                    var lists = node?["data"]?.AsArray();
                    
                    if (lists != null && lists.Count > 0)
                    {
                        string? preseedVCsListId = null;
                        string? startupFundraisingListId = null;
                        
                        foreach (var list in lists)
                        {
                            string name = list?["name"]?.ToString() ?? "Unknown";
                            string listId = list?["id"]?["list_id"]?.ToString() ?? "unknown";
                            
                            if (name.Contains("Preseed VCs from Notion", StringComparison.OrdinalIgnoreCase))
                            {
                                preseedVCsListId = listId;
                            }
                            else if (name.Contains("Startup Fundraising", StringComparison.OrdinalIgnoreCase))
                            {
                                startupFundraisingListId = listId;
                            }
                        }
                        
                        return (preseedVCsListId, startupFundraisingListId);
                    }
                }
                return (null, null);
            }
            catch
            {
                return (null, null);
            }
        }
    }

    static async Task TestAttioList()
    {
        Console.WriteLine("📝 Testing Attio database lookup for both target lists...");
        
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
    }

    static async Task<string?> CreateNotionInvestorEntry(string domain, string name, string markdownContent)
    {
        string notionToken = Environment.GetEnvironmentVariable("NOTION_API_KEY");
        if (string.IsNullOrEmpty(notionToken))
        {
            return null;
        }

        string databaseId = NOTION_INVESTOR_RESEARCH_DATABASE_ID;

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {notionToken}");
            client.DefaultRequestHeaders.Add("Notion-Version", "2022-06-28");

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

                string createJson = System.Text.Json.JsonSerializer.Serialize(createPageBody);
                var createContent = new StringContent(createJson, Encoding.UTF8, "application/json");
                
                HttpResponseMessage createResponse = await client.PostAsync("https://api.notion.com/v1/pages", createContent);
                string createResponseBody = await createResponse.Content.ReadAsStringAsync();
                
                if (createResponse.IsSuccessStatusCode)
                {
                    // Parse response to get the page ID
                    JsonNode createNode = JsonNode.Parse(createResponseBody);
                    string pageId = createNode?["id"]?.ToString();
                    
                    if (!string.IsNullOrEmpty(pageId))
                    {
                        // Step 2: Add markdown content to the page using Mark2Notion API
                        // Create a new HttpClient for Mark2Notion API
                        using (HttpClient mark2NotionClient = new HttpClient())
                        {
                            // Get Mark2Notion API key from environment variable
                            string mark2NotionApiKey = Environment.GetEnvironmentVariable("MARK2NOTION_API_KEY");
                            if (string.IsNullOrEmpty(mark2NotionApiKey))
                            {
                                Console.WriteLine("MARK2NOTION_API_KEY environment variable not set");
                                return null;
                            }

                            // Set up the Mark2Notion API request
                            mark2NotionClient.DefaultRequestHeaders.Add("x-api-key", mark2NotionApiKey);

                            var mark2NotionRequestBody = new
                            {
                                markdown = markdownContent,
                                notionToken = notionToken,
                                pageId = pageId
                                // Optional: after parameter can be used to append after specific block
                                // after = "block-id-to-append-after"
                            };

                            string mark2NotionJson = System.Text.Json.JsonSerializer.Serialize(mark2NotionRequestBody);
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
                                    return pageId; // Success - return the page ID
                                }
                                else
                                {
                                    Console.WriteLine($"Mark2Notion API returned status: {status}");
                                    return null;
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Mark2Notion API error: {mark2NotionResponse.StatusCode}");
                                Console.WriteLine($"Response: {mark2NotionResponseBody}");
                                return null;
                            }
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
    }

    static string ExtractVCNameFromResponse(string response)
    {
        // Look for "VC Name: [name]" at the beginning of the response
        var lines = response.Split('\n');
        if (lines.Length > 0)
        {
            string firstLine = lines[0].Trim();
            if (firstLine.StartsWith("VC Name:", StringComparison.OrdinalIgnoreCase))
            {
                string vcName = firstLine.Substring(8).Trim(); // Remove "VC Name:" prefix
                
                // Clean markdown formatting (remove ** and other markdown symbols)
                vcName = vcName.Replace("**", "").Replace("*", "").Trim();
                
                if (!string.IsNullOrEmpty(vcName))
                {
                    return vcName;
                }
            }
        }
        
        // Fallback: derive from domain if parsing fails
        return "Unknown VC";
    }


    static async Task TestNotionInsert()
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
        string? pageId = await CreateNotionInvestorEntry(testDomain, testName, testMarkdown);
        
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
    
    static async Task FixAttioLinks(string investorDomain)
    {
        try
        {
            Console.WriteLine($"🔗 Fixing Attio links for {investorDomain}...");
            
            // Step 1: Look up existing Notion research page
            Console.WriteLine($"🔍 Looking up existing Notion research for {investorDomain}...");
            string? notionUrl = await FindExistingNotionResearch(investorDomain);
            
            if (notionUrl == null)
            {
                Console.WriteLine($"❌ No existing Notion research found for {investorDomain}");
                Console.WriteLine("   Use the regular workflow to create new research first.");
                return;
            }
            
            Console.WriteLine($"✅ Found existing Notion research: {notionUrl}");
            
            // Step 2: Update Attio records with the URL (skip Perplexity and Notion creation)
            Console.WriteLine($"🔄 Updating Attio database links...");
            await UpdateAttioCRM("fix-links-mode", investorDomain, notionUrl);
            
            Console.WriteLine($"🎉 Successfully updated Attio links for {investorDomain}!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error fixing links for {investorDomain}: {ex.Message}");
        }
    }
    
    static async Task<string?> FindExistingNotionResearch(string investorDomain)
    {
        string notionToken = Environment.GetEnvironmentVariable("NOTION_API_KEY");
        if (string.IsNullOrEmpty(notionToken))
        {
            return null;
        }

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {notionToken}");
            client.DefaultRequestHeaders.Add("Notion-Version", "2022-06-28");

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
                
                string searchJson = System.Text.Json.JsonSerializer.Serialize(searchBody);
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
                            // Return the Notion URL
                            return $"https://notion.so/{pageId.Replace("-", "")}";
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
    }
}
