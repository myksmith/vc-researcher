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
    // Note: We now update company records directly instead of list-specific records

    // Singleton HTTP Clients
    private static HttpClient? _notionClient;
    private static HttpClient? _attioClient;
    private static HttpClient? _perplexityClient;

    // HTTP Client Helper Methods (Singletons)
    static HttpClient GetNotionClient()
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

    static HttpClient GetAttioClient()
    {
        if (_attioClient == null)
        {
            string attioToken = Environment.GetEnvironmentVariable("ATTIO_API_KEY");
            if (string.IsNullOrEmpty(attioToken))
            {
                throw new InvalidOperationException("ATTIO_API_KEY environment variable not set");
            }

            _attioClient = new HttpClient();
            _attioClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {attioToken}");
        }
        return _attioClient;
    }

    static HttpClient GetPerplexityClient()
    {
        if (_perplexityClient == null)
        {
            string apiKey = Environment.GetEnvironmentVariable("SONAR_API_KEY");
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("SONAR_API_KEY environment variable not set");
            }

            _perplexityClient = new HttpClient();
            _perplexityClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        }
        return _perplexityClient;
    }

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
            Console.WriteLine("       dotnet run --force-research <investor-domain>");
            Console.WriteLine("       dotnet run --regen-research <investor-domain>");
            Console.WriteLine("\nExamples:");
            Console.WriteLine("  dotnet run example-vc.com                    # Create research (aborts if already exists)");
            Console.WriteLine("  dotnet run --force-research example-vc.com   # Create research even if duplicates exist");
            Console.WriteLine("  dotnet run --regen-research example-vc.com   # Delete existing research and create new one");
            Console.WriteLine("\nTest commands:");
            Console.WriteLine("  dotnet run --test-notion      # Test Notion API connection");
            Console.WriteLine("  dotnet run --test-notion-insert # Test Notion database entry creation with markdown");
            Console.WriteLine("  dotnet run --ping-attio       # Ping Attio API for basic connectivity");
            Console.WriteLine("  dotnet run --test-attio-list  # Test Attio list lookup for both target databases");
            Console.WriteLine("\nUtility commands:");
            Console.WriteLine("  dotnet run --fix-links <domain> # Update Attio with existing Notion research URL (no new research)");
            Console.WriteLine("  dotnet run --research-only-no-links <domain> # Create new research in Notion only (no Attio updates)");
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
        
        if (args[0] == "--research-only-no-links")
        {
            if (args.Length < 2)
            {
                Console.WriteLine("❌ Usage: dotnet run --research-only-no-links <investor-domain>");
                Console.WriteLine("Example: dotnet run --research-only-no-links sequoiacap.com");
                return;
            }

            string domain = args[1];
            await ResearchOnlyNoLinks(domain);
            return;
        }

        if (args[0] == "--regen-research")
        {
            if (args.Length < 2)
            {
                Console.WriteLine("❌ Usage: dotnet run --regen-research <investor-domain>");
                Console.WriteLine("Example: dotnet run --regen-research sequoiacap.com");
                return;
            }

            string domain = args[1];
            await RegenerateResearch(domain);
            return;
        }

        // Parse arguments for force-research flag
        bool forceResearch = false;
        string investorDomain;

        if (args[0] == "--force-research")
        {
            if (args.Length < 2)
            {
                Console.WriteLine("❌ Usage: dotnet run --force-research <investor-domain>");
                Console.WriteLine("Example: dotnet run --force-research sequoiacap.com");
                return;
            }
            forceResearch = true;
            investorDomain = args[1];
        }
        else
        {
            investorDomain = args[0];
        }

        try
        {
            // Step 0: Check if research already exists (unless force flag is used)
            if (!forceResearch)
            {
                Console.WriteLine($"🔍 Checking if research already exists for {investorDomain}...");
                bool domainExists = await CheckNotionDomainExists(investorDomain);
                
                if (domainExists)
                {
                    Console.WriteLine($"✅ Research already exists for {investorDomain} in Notion.");
                    Console.WriteLine($"ℹ️  Use --force-research flag to create duplicate research anyway:");
                    Console.WriteLine($"   dotnet run --force-research {investorDomain}");
                    return;
                }
                Console.WriteLine($"✅ No existing research found for {investorDomain}, proceeding...");
            }
            else
            {
                Console.WriteLine($"⚠️  Force research mode enabled - will create research even if duplicates exist");
            }
            
            // Step 1: Validate both systems are accessible BEFORE doing expensive Perplexity call
            Console.WriteLine($"🔍 Validating systems for {investorDomain}...");

            string? notionDbOk = await ValidateNotionDatabase();
            string attioCompanyId = await FindAttioRecord(investorDomain);

            // Early exit if either system is not available
            if (notionDbOk == null)
            {
                Console.WriteLine($"❌ Could not access Notion Investor Research database");
                return;
            }

            if (attioCompanyId == null)
            {
                Console.WriteLine($"❌ Could not find Attio company record for {investorDomain}");
                return;
            }

            Console.WriteLine("✅ Both Notion database and Attio company record are accessible");

            // Step 2: Get analysis from Perplexity (only after confirming records exist)
            JsonNode? perplexityJson = await QueryPerplexityForVCAnalysis(investorDomain);
            if (perplexityJson == null)
            {
                Console.WriteLine("❌ Failed to get analysis from Perplexity");
                return;
            }
            Console.WriteLine("✅ Completed Perplexity analysis");

            // Step 3: Create Notion research page
            string? notionPageUrl = await UpdateNotionDatabase("validated", investorDomain, perplexityJson);
            if (notionPageUrl == null)
            {
                Console.WriteLine("❌ Failed to create Notion page - cannot update Attio with URL");
                return;
            }
            Console.WriteLine("✅ Created Notion research page");

            // Step 4: Update Attio company record with the Notion URL
            await UpdateAttioCRM(attioCompanyId, investorDomain, notionPageUrl);
            Console.WriteLine("✅ Updated Attio company record");

            Console.WriteLine($"🎉 Successfully processed {investorDomain}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error processing {investorDomain}: {ex.Message}");
        }
    }

    static async Task<JsonNode?> QueryPerplexityForVCAnalysis(string investorDomain)
    {
        string apiUrl = "https://api.perplexity.ai/chat/completions";

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

        try
        {
            HttpClient client = GetPerplexityClient();

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
                return node; // Return the full JSON response
            }
            catch (Exception ex)
            {
                throw new Exception($"Perplexity API error: {ex.Message}");
            }
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"❌ {ex.Message}");
            return null;
        }
    }

    static async Task<string?> ValidateNotionDatabase()
    {
        try
        {
            HttpClient client = GetNotionClient();

            try
            {
                Console.WriteLine("🔍 Validating Notion Investor Research database...");
                
                // Try to query the Investor Research database to validate it exists and is accessible
                var queryBody = new
                {
                    page_size = 1 // Just get one record to validate access
                };
                
                string queryJson = System.Text.Json.JsonSerializer.Serialize(queryBody);
                var queryContent = new StringContent(queryJson, Encoding.UTF8, "application/json");
                
                HttpResponseMessage response = await client.PostAsync($"https://api.notion.com/v1/databases/{NOTION_INVESTOR_RESEARCH_DATABASE_ID}/query", queryContent);
                string responseBody = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("✅ Notion Investor Research database is accessible");
                    return "database-validated"; // Return success indicator
                }
                else
                {
                    Console.WriteLine($"❌ Failed to access Notion database: {response.StatusCode}");
                    Console.WriteLine($"Response: {responseBody}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error validating Notion database: {ex.Message}");
                return null;
            }
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"❌ {ex.Message}");
            return null;
        }
    }

    static async Task<string?> FindAttioRecord(HttpClient client, string investorDomain)
    {
        try
        {
            Console.WriteLine($"🔍 Searching for company record matching {investorDomain}...");
            
            // Query Attio companies using domains filter
            var queryBody = new
            {
                filter = new
                {
                    domains = investorDomain
                }
            };
            
            string queryJson = System.Text.Json.JsonSerializer.Serialize(queryBody);
            var queryContent = new StringContent(queryJson, Encoding.UTF8, "application/json");
            
            HttpResponseMessage response = await client.PostAsync("https://api.attio.com/v2/objects/companies/records/query", queryContent);
            string responseBody = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                JsonNode node = JsonNode.Parse(responseBody);
                var records = node?["data"]?.AsArray();
                
                if (records != null && records.Count > 0)
                {
                    // Get the first matching record
                    var firstRecord = records[0];
                    string? recordId = firstRecord?["id"]?["record_id"]?.ToString();
                    string? companyName = firstRecord?["values"]?["name"]?.AsArray()?[0]?["value"]?.ToString();
                    
                    if (!string.IsNullOrEmpty(recordId))
                    {
                        Console.WriteLine($"✅ Found company record: {companyName ?? "Unknown"} (ID: {recordId})");
                        return recordId;
                    }
                }
            }
            else
            {
                Console.WriteLine($"❌ Failed to search company records: {response.StatusCode}");
                Console.WriteLine($"Response: {responseBody}");
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error searching Attio company records: {ex.Message}");
            return null;
        }
    }
    
    // Wrapper for main workflow validation
    static async Task<string> FindAttioRecord(string investorDomain)
    {
        try
        {
            HttpClient client = GetAttioClient();
            return await FindAttioRecord(client, investorDomain);
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"❌ {ex.Message}");
            return null;
        }
    }

    static string RenderPerplexityJsonToNotion(JsonNode perplexityJson)
    {
        // Extract the chat content from the JSON response
        string content = perplexityJson["choices"][0]["message"]["content"].ToString();
        
        // Escape dollar signs to prevent them from being interpreted as LaTeX math or other special formatting in Notion
        string escapedContent = content.Replace("$", "\\$");
        
        return escapedContent;
    }
    
    static async Task<string?> UpdateNotionDatabase(string recordId, string investorDomain, JsonNode perplexityJson)
    {
        // Render the JSON to markdown content
        string analysis = RenderPerplexityJsonToNotion(perplexityJson);
        
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
        try
        {
            HttpClient client = GetAttioClient();

            try
            {
                Console.WriteLine($"🔍 Searching for {investorDomain} in Attio company records...");
                
                // Step 1: Find the company record by searching
                string? companyRecordId = await FindAttioRecord(client, investorDomain);
                
                if (companyRecordId == null)
                {
                    Console.WriteLine($"⚠️  No company records found for {investorDomain}");
                    return;
                }
                
                // Step 2: Update the company record with the found record ID
                bool updated = await UpdateAttioCompanyRecord(client, companyRecordId, notionUrl);
                
                if (updated)
                {
                    Console.WriteLine($"✅ Successfully updated Notion Research URL for {investorDomain}");
                }
                else
                {
                    Console.WriteLine($"❌ Failed to update company record");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error updating Attio records: {ex.Message}");
            }
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"❌ {ex.Message}");
        }
    }
    
    static async Task<bool> UpdateAttioCompanyRecord(HttpClient client, string recordId, string notionUrl)
    {
        try
        {
            Console.WriteLine($"🔄 Updating company record (ID: {recordId}) with Notion URL...");
            
            var updateBody = new
            {
                data = new
                {
                    values = new Dictionary<string, object[]>
                    {
                        ["notion_research_url"] = new object[]
                        {
                            new
                            {
                                value = notionUrl
                            }
                        }
                    }
                }
            };
            
            string updateJson = System.Text.Json.JsonSerializer.Serialize(updateBody);
            var updateContent = new StringContent(updateJson, Encoding.UTF8, "application/json");
            
            HttpResponseMessage updateResponse = await client.PatchAsync($"https://api.attio.com/v2/objects/companies/records/{recordId}", updateContent);
            string updateResponseBody = await updateResponse.Content.ReadAsStringAsync();
            
            if (updateResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"✅ Successfully updated company with Notion Research URL");
                return true;
            }
            else
            {
                Console.WriteLine($"❌ Failed to update company record: {updateResponse.StatusCode}");
                Console.WriteLine($"Response: {updateResponseBody}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error updating company record: {ex.Message}");
            return false;
        }
    }

    // Test functions for API connectivity
    static async Task TestNotionConnection()
    {
        Console.WriteLine("🧪 Testing Notion API connection...");

        try
        {
            HttpClient client = GetNotionClient();

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
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"❌ {ex.Message}");
        }
    }

    static async Task PingAttio()
    {
        Console.WriteLine("🏓 Pinging Attio API...");

        try
        {
            HttpClient client = GetAttioClient();

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

    // Note: FindAttioBothLists function removed since we now work with company records directly

    static async Task TestAttioList()
    {
        Console.WriteLine("📝 Testing Attio database lookup for both target lists...");

        try
        {
            HttpClient client = GetAttioClient();

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

    static async Task<string?> CreateNotionInvestorEntry(string domain, string name, string markdownContent)
    {
        string databaseId = NOTION_INVESTOR_RESEARCH_DATABASE_ID;

        try
        {
            HttpClient client = GetNotionClient();
            string notionToken = Environment.GetEnvironmentVariable("NOTION_API_KEY");

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
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"❌ {ex.Message}");
            return null;
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
    
    static async Task ResearchOnlyNoLinks(string investorDomain)
    {
        try
        {
            Console.WriteLine($"🔍 Research-only mode for {investorDomain} (no Attio updates)...");
            
            // Step 1: Get analysis from Perplexity
            Console.WriteLine($"🧠 Querying Perplexity for VC analysis...");
            JsonNode? perplexityJson = await QueryPerplexityForVCAnalysis(investorDomain);
            if (perplexityJson == null)
            {
                Console.WriteLine("❌ Failed to get analysis from Perplexity");
                return;
            }
            Console.WriteLine("✅ Completed Perplexity analysis");
            
            // Step 2: Create Notion research entry
            Console.WriteLine($"📝 Creating Notion research entry...");
            string? notionUrl = await UpdateNotionDatabase("research-only-mode", investorDomain, perplexityJson);
            
            if (notionUrl != null)
            {
                Console.WriteLine($"✅ Successfully created Notion research entry: {notionUrl}");
                Console.WriteLine($"📊 Research completed for {investorDomain} - no Attio updates performed");
            }
            else
            {
                Console.WriteLine($"❌ Failed to create Notion research entry for {investorDomain}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in research-only mode for {investorDomain}: {ex.Message}");
        }
    }
    
    static async Task<bool> CheckNotionDomainExists(string investorDomain)
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
                
                string searchJson = System.Text.Json.JsonSerializer.Serialize(searchBody);
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
    
    static async Task<string?> FindExistingNotionResearch(string investorDomain)
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
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"❌ {ex.Message}");
            return null;
        }
    }

    static async Task RegenerateResearch(string investorDomain)
    {
        try
        {
            Console.WriteLine($"🔄 Regenerating research for {investorDomain}...");

            // TODO: Implement the following steps:
            // 1. Find existing Notion research page by domain
            // 2. Delete the existing Notion page
            // 3. Run the full research workflow (Perplexity + create new Notion entry + update Attio)

            Console.WriteLine("⚠️  This feature is not yet implemented.");
            Console.WriteLine("   Coming soon: Will delete existing research and create a fresh analysis.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error regenerating research for {investorDomain}: {ex.Message}");
        }
    }
}
