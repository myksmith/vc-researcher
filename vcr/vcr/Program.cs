using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Nodes;
using System.Text.Json;

class Program
{
    static async Task Main(string[] args)
    {
        string apiUrl = "https://api.perplexity.ai/chat/completions"; // Replace with the actual API endpoint
        string apiKey = ""; // Replace with your API key

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
                        content = "What were the results of the 2025 French Open Finals?"
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
                String chatResponse = node["choices"][0]["message"]["content"].ToString();
                    
                Console.WriteLine("Chat response only: " + chatResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }
}