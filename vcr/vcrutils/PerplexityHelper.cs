using System;
using System.Net.Http;

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
    }
}
