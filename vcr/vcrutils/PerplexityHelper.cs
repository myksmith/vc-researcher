using System;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;

namespace vcrutils
{
    public static class PerplexityHelper
    {
        // Singleton HTTP Client
        private static HttpClient? _perplexityClient;

        // HTTP Client Helper Method (Singleton)
        public static HttpClient GetPerplexityClient()
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

        /// <summary>
        /// Extracts sources/citations from Perplexity API response and formats them as markdown
        /// </summary>
        /// <param name="perplexityJson">The JSON response from Perplexity API</param>
        /// <returns>Markdown-formatted sources section, or empty string if no sources found</returns>
        public static string ExtractSourcesAsMarkdown(JsonNode perplexityJson)
        {
            try
            {
                var citations = perplexityJson["citations"]?.AsArray();
                var searchResults = perplexityJson["search_results"]?.AsArray();

                if ((citations == null || citations.Count == 0) && (searchResults == null || searchResults.Count == 0))
                {
                    return string.Empty;
                }

                var markdown = new StringBuilder();
                markdown.AppendLine("\n## Sources\n");

                // If we have search_results with detailed metadata, use those
                if (searchResults != null && searchResults.Count > 0)
                {
                    int index = 1;
                    foreach (var result in searchResults)
                    {
                        string? title = result?["title"]?.ToString();
                        string? url = result?["url"]?.ToString();
                        string? date = result?["date"]?.ToString();
                        string? snippet = result?["snippet"]?.ToString();

                        if (!string.IsNullOrEmpty(url))
                        {
                            markdown.Append($"{index}. ");

                            if (!string.IsNullOrEmpty(title))
                            {
                                markdown.Append($"[{title}]({url})");
                            }
                            else
                            {
                                markdown.Append($"[{url}]({url})");
                            }

                            if (!string.IsNullOrEmpty(date))
                            {
                                markdown.Append($" - {date}");
                            }

                            markdown.AppendLine();

                            // Optionally include snippet as indented quote
                            if (!string.IsNullOrEmpty(snippet) && snippet.Length > 0)
                            {
                                // Limit snippet length for readability
                                string trimmedSnippet = snippet.Length > 200 ? snippet.Substring(0, 200) + "..." : snippet;
                                markdown.AppendLine($"   - {trimmedSnippet}");
                            }

                            index++;
                        }
                    }
                }
                // Otherwise fall back to simple citations list
                else if (citations != null && citations.Count > 0)
                {
                    int index = 1;
                    foreach (var citation in citations)
                    {
                        string? url = citation?.ToString();
                        if (!string.IsNullOrEmpty(url))
                        {
                            markdown.AppendLine($"{index}. [{url}]({url})");
                            index++;
                        }
                    }
                }

                return markdown.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to extract sources from Perplexity response: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
