using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Nodes;
using System.Text.Json;
using vcrutils;

namespace VCR
{
    /// <summary>
    /// Main program class for processing venture capital research using Perplexity, Notion, and Attio APIs.
    /// </summary>
    class Program
    {
        // Note: We now update company records directly instead of list-specific records

        /// <summary>
        /// Main entry point for the application.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
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
                await TestCommands.TestNotionConnection();
                return;
            }

            if (args[0] == "--test-notion-insert")
            {
                await TestCommands.TestNotionInsert();
                return;
            }

            if (args[0] == "--ping-attio")
            {
                await TestCommands.PingAttio();
                return;
            }

            if (args[0] == "--test-attio-list")
            {
                await TestCommands.TestAttioList();
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
                    bool domainExists = await NotionHelper.CheckNotionDomainExists(investorDomain);
                    
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
                string attioCompanyId = await AttioHelper.FindAttioRecord(investorDomain);

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

                // Step 5: Add Perplexity research as a note to the Attio company record
                await AddNoteToAttioRecord(attioCompanyId, perplexityJson);

                Console.WriteLine($"🎉 Successfully processed {investorDomain}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error processing {investorDomain}: {ex.Message}");
            }
        }

        /// <summary>
        /// Queries the Perplexity API for venture capital analysis.
        /// </summary>
        /// <param name="investorDomain">The domain of the investor to analyze.</param>
        /// <returns>A JSON node containing the analysis results, or null if the query fails.</returns>
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
                HttpClient client = PerplexityHelper.GetPerplexityClient();

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

        /// <summary>
        /// Validates the Notion Investor Research database.
        /// </summary>
        /// <returns>A string indicating validation success, or null if validation fails.</returns>
        static async Task<string?> ValidateNotionDatabase()
        {
            try
            {
                HttpClient client = NotionHelper.GetNotionClient();

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
                    
                    HttpResponseMessage response = await client.PostAsync($"https://api.notion.com/v1/databases/{NotionHelper.NOTION_INVESTOR_RESEARCH_DATABASE_ID}/query", queryContent);
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

        /// <summary>
        /// Renders the Perplexity JSON response to markdown format.
        /// </summary>
        /// <param name="perplexityJson">The JSON response from Perplexity API.</param>
        /// <returns>A string containing the markdown-formatted content.</returns>
        static string RenderPerplexityJsonToMarkdown(JsonNode perplexityJson)
        {
            // Extract the chat content from the JSON response
            string content = perplexityJson["choices"][0]["message"]["content"].ToString();

            // Extract sources from the JSON response
            string sources = PerplexityHelper.ExtractSourcesAsMarkdown(perplexityJson);

            // Combine content and sources
            string fullContent = content;
            if (!string.IsNullOrEmpty(sources))
            {
                fullContent += "\n\n" + sources;
            }

            // Escape dollar signs to prevent them from being interpreted as LaTeX math or other special formatting in Notion
            string escapedContent = fullContent.Replace("$", "\\$");

            return escapedContent;
        }

        /// <summary>
        /// Adds a Perplexity research note to an Attio company record.
        /// </summary>
        /// <param name="attioCompanyId">The ID of the Attio company record.</param>
        /// <param name="perplexityJson">The JSON response from Perplexity API.</param>
        static async Task AddNoteToAttioRecord(string attioCompanyId, JsonNode perplexityJson)
        {
            // Step 5: Add Perplexity research as a note to the Attio company record
            string perplexityMarkdown = RenderPerplexityJsonToMarkdown(perplexityJson);
            // Remove the dollar sign escaping since Attio doesn't need it
            string attioMarkdown = perplexityMarkdown.Replace("\\$", "$");

            // Debug: Dump the markdown to console
            Console.WriteLine("========== MARKDOWN OUTPUT START ==========");
            Console.WriteLine(attioMarkdown);
            Console.WriteLine("========== MARKDOWN OUTPUT END ==========");

            Console.WriteLine("📝 Adding Perplexity research note to Attio...");
            bool noteCreated = await AttioHelper.CreateAttioNote(attioCompanyId, "Perplexity Research", attioMarkdown, NoteFormat.Markdown);

            if (noteCreated)
            {
                Console.WriteLine("✅ Added Perplexity research note to Attio");
            }
            else
            {
                Console.WriteLine("⚠️  Failed to add Perplexity research note to Attio");
            }
        }
        
        /// <summary>
        /// Updates the Notion database with Perplexity analysis.
        /// </summary>
        /// <param name="recordId">The record ID for the update.</param>
        /// <param name="investorDomain">The domain of the investor.</param>
        /// <param name="perplexityJson">The JSON response from Perplexity API.</param>
        /// <returns>The URL of the created Notion page, or null if creation fails.</returns>
        static async Task<string?> UpdateNotionDatabase(string recordId, string investorDomain, JsonNode perplexityJson)
        {
            // Render the JSON to markdown content
            string analysis = RenderPerplexityJsonToMarkdown(perplexityJson);
            
            // Extract VC name from the analysis response
            string vcName = ExtractVCNameFromResponse(analysis);
            
            // If extraction failed, fall back to domain-based name
            if (vcName == "Unknown VC")
            {
                vcName = investorDomain.Replace(".com", "").Replace(".vc", "").Replace(".", " ");
                vcName = char.ToUpper(vcName[0]) + vcName.Substring(1);
            }
            
            string? pageId = await NotionHelper.CreateNotionInvestorEntry(investorDomain, vcName, analysis);
            
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

        /// <summary>
        /// Updates the Attio CRM with the Notion research URL.
        /// </summary>
        /// <param name="recordId">The record ID for the update.</param>
        /// <param name="investorDomain">The domain of the investor.</param>
        /// <param name="notionUrl">The URL of the Notion research page.</param>
        static async Task UpdateAttioCRM(string recordId, string investorDomain, string notionUrl)
        {
            try
            {
                HttpClient client = AttioHelper.GetAttioClient();

                try
                {
                    Console.WriteLine($"🔍 Searching for {investorDomain} in Attio company records...");
                    
                    // Step 1: Find the company record by searching
                    string? companyRecordId = await AttioHelper.FindAttioRecord(client, investorDomain);

                    if (companyRecordId == null)
                    {
                        Console.WriteLine($"⚠️  No company records found for {investorDomain}");
                        return;
                    }

                    // Step 2: Update the company record with the found record ID
                    bool updated = await AttioHelper.UpdateAttioCompanyRecord(client, companyRecordId, notionUrl);
                    
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

        /// <summary>
        /// Extracts the VC name from the Perplexity analysis response.
        /// </summary>
        /// <param name="response">The markdown-formatted analysis response.</param>
        /// <returns>The extracted VC name, or "Unknown VC" if extraction fails.</returns>
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

        /// <summary>
        /// Fixes Attio links by updating them with existing Notion research URLs.
        /// </summary>
        /// <param name="investorDomain">The domain of the investor.</param>
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

        /// <summary>
        /// Performs research-only mode by creating new research in Notion without updating Attio.
        /// </summary>
        /// <param name="investorDomain">The domain of the investor.</param>
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

        /// <summary>
        /// Finds the existing Notion research URL for a given investor domain.
        /// </summary>
        /// <param name="investorDomain">The domain of the investor.</param>
        /// <returns>The URL of the existing Notion research page, or null if not found.</returns>
        static async Task<string?> FindExistingNotionResearch(string investorDomain)
        {
            string? pageId = await NotionHelper.FindExistingNotionPageId(investorDomain);
            if (!string.IsNullOrEmpty(pageId))
            {
                return $"https://notion.so/{pageId.Replace("-", "")}";
            }
            return null;
        }

        /// <summary>
        /// Regenerates research for a given investor domain by deleting existing research and creating new entries.
        /// </summary>
        /// <param name="investorDomain">The domain of the investor.</param>
        static async Task RegenerateResearch(string investorDomain)
        {
            try
            {
                Console.WriteLine($"🔄 Regenerating research for {investorDomain}...");

                // Step 1: Find existing Notion research page by domain
                Console.WriteLine($"🔍 Searching for existing research...");
                string? existingPageId = await NotionHelper.FindExistingNotionPageId(investorDomain);

                if (existingPageId == null)
                {
                    Console.WriteLine($"⚠️  No existing research found for {investorDomain}");
                    Console.WriteLine($"   Use the regular workflow instead: dotnet run {investorDomain}");
                    return;
                }

                Console.WriteLine($"✅ Found existing research (Page ID: {existingPageId})");

                // Step 2: Delete the existing Notion page
                bool deleted = await NotionHelper.DeleteNotionPage(existingPageId);
                if (!deleted)
                {
                    Console.WriteLine($"❌ Failed to delete existing research - aborting regeneration");
                    return;
                }

                // Step 3: Validate both systems are accessible BEFORE doing expensive Perplexity call
                Console.WriteLine($"🔍 Validating systems for {investorDomain}...");

                string? notionDbOk = await ValidateNotionDatabase();
                string attioCompanyId = await AttioHelper.FindAttioRecord(investorDomain);

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

                // Step 4: Get analysis from Perplexity
                JsonNode? perplexityJson = await QueryPerplexityForVCAnalysis(investorDomain);
                if (perplexityJson == null)
                {
                    Console.WriteLine("❌ Failed to get analysis from Perplexity");
                    return;
                }
                Console.WriteLine("✅ Completed Perplexity analysis");

                // Step 5: Create new Notion research page
                string? notionPageUrl = await UpdateNotionDatabase("regenerated", investorDomain, perplexityJson);
                if (notionPageUrl == null)
                {
                    Console.WriteLine("❌ Failed to create new Notion page - cannot update Attio with URL");
                    return;
                }
                Console.WriteLine("✅ Created new Notion research page");

                // Step 6: Update Attio company record with the new Notion URL
                await UpdateAttioCRM(attioCompanyId, investorDomain, notionPageUrl);
                Console.WriteLine("✅ Updated Attio company record");

                // Step 7: Add Perplexity research as a note to the Attio company record
                await AddNoteToAttioRecord(attioCompanyId, perplexityJson);

                Console.WriteLine($"🎉 Successfully regenerated research for {investorDomain}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error regenerating research for {investorDomain}: {ex.Message}");
            }
        }
    }
}
