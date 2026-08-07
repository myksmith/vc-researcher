using System.Text.Json.Nodes;

namespace vcrutils
{
    public static class JsonParser
    {
        /// <summary>
        /// Safely extracts a JSON array from a JSON node.
        /// </summary>
        /// <param name="node">The JSON node to extract the array from.</param>
        /// <param name="key">The key of the array in the JSON node.</param>
        /// <returns>The extracted JSON array, or null if not found.</returns>
        public static JsonArray? GetArray(JsonNode node, string key)
        {
            return node[key]?.AsArray();
        }

        /// <summary>
        /// Safely extracts a string value from a JSON node.
        /// </summary>
        /// <param name="node">The JSON node to extract the string from.</param>
        /// <param name="key">The key of the string in the JSON node.</param>
        /// <returns>The extracted string value, or null if not found.</returns>
        public static string? GetString(JsonNode node, string key)
        {
            return node[key]?.ToString();
        }

        /// <summary>
        /// Safely extracts a string value from a JSON node, with a default value.
        /// </summary>
        /// <param name="node">The JSON node to extract the string from.</param>
        /// <param name="key">The key of the string in the JSON node.</param>
        /// <param name="defaultValue">The default value to return if the key is not found.</param>
        /// <returns>The extracted string value, or the default value if not found.</returns>
        public static string GetString(JsonNode node, string key, string defaultValue)
        {
            return node[key]?.ToString() ?? defaultValue;
        }
    }
}
