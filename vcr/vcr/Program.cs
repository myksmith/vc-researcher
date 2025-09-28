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
        // TODO: Search Attio CRM for matching record  
        // Return record ID if found, null if not found
        await Task.Delay(100); // Simulate API call
        
        // Stubbed: Always return a mock record ID for now
        return $"attio-record-{investorDomain.Replace(".", "-")}";
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
}
