using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace vcrutils
{
    public enum NoteFormat
    {
        Plaintext,
        Markdown
    }

    /// <summary>
    /// Provides helper methods for interacting with the Attio API.
    /// </summary>
    public static class AttioHelper
    {
        // Singleton HTTP Client
        private static HttpClient? _attioClient;

        /// <summary>
        /// Gets a singleton instance of the HTTP client configured for the Attio API.
        /// </summary>
        /// <returns>A configured HttpClient instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the ATTIO_API_KEY environment variable is not set.</exception>
        public static HttpClient GetAttioClient()
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

        /// <summary>
        /// Finds an Attio company record by investor domain using the provided HTTP client.
        /// </summary>
        /// <param name="client">The HTTP client to use for the request.</param>
        /// <param name="investorDomain">The domain of the investor to search for.</param>
        /// <returns>The record ID if found, null otherwise.</returns>
        public static async Task<string?> FindAttioRecord(HttpClient client, string investorDomain)
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

                string queryJson = JsonSerializer.Serialize(queryBody);
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

        /// <summary>
        /// Finds an Attio company record by investor domain.
        /// </summary>
        /// <param name="investorDomain">The domain of the investor to search for.</param>
        /// <returns>The record ID if found, null otherwise.</returns>
        public static async Task<string?> FindAttioRecord(string investorDomain)
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

        /// <summary>
        /// Updates an Attio company record with the Notion research URL.
        /// </summary>
        /// <param name="client">The HTTP client to use for the request.</param>
        /// <param name="recordId">The ID of the record to update.</param>
        /// <param name="notionUrl">The Notion research URL to add to the record.</param>
        /// <returns>True if the update was successful, false otherwise.</returns>
        public static async Task<bool> UpdateAttioCompanyRecord(HttpClient client, string recordId, string notionUrl)
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

                string updateJson = JsonSerializer.Serialize(updateBody);
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

        /// <summary>
        /// Creates a note in Attio for a specified company record.
        /// </summary>
        /// <param name="recordId">The ID of the company record to associate the note with.</param>
        /// <param name="titleString">The title of the note.</param>
        /// <param name="contentString">The content of the note.</param>
        /// <param name="format">The format of the note content (Plaintext or Markdown).</param>
        /// <returns>True if the note was successfully created, false otherwise.</returns>
        public static async Task<bool> CreateAttioNote(string recordId, string titleString, string contentString, NoteFormat format)
        {
            try
            {
                HttpClient client = GetAttioClient();

                string formatString = format == NoteFormat.Markdown ? "markdown" : "plaintext";

                var requestBody = new
                {
                    data = new
                    {
                        parent_object = "companies",
                        parent_record_id = recordId,
                        title = titleString,
                        format = formatString,
                        content = contentString
                    }
                };

                string jsonBody = JsonSerializer.Serialize(requestBody);
                var httpContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync("https://api.attio.com/v2/notes", httpContent);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Note created successfully");
                    return true;
                }
                else
                {
                    Console.WriteLine($"Failed to create note: {response.StatusCode}");
                    Console.WriteLine($"Response: {responseBody}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating note: {ex.Message}");
                return false;
            }
        }
    }
}
