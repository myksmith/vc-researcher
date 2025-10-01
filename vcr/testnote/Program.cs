using System;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using vcrutils;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  testnote <domain> <note-title> <note-content>");
            Console.WriteLine("  testnote --test-sources <json-file-path>");
            Console.WriteLine("\nExamples:");
            Console.WriteLine("  testnote example-vc.com \"Meeting Notes\" \"Discussion about investment thesis\"");
            Console.WriteLine("  testnote --test-sources perplexity-response.json");
            return;
        }

        // Handle --test-sources command
        if (args[0] == "--test-sources")
        {
            if (args.Length < 2)
            {
                Console.WriteLine("❌ Error: Please provide a JSON file path");
                Console.WriteLine("Usage: testnote --test-sources <json-file-path>");
                return;
            }

            await TestSourcesExtraction(args[1]);
            return;
        }

        // Handle regular note creation
        if (args.Length < 3)
        {
            Console.WriteLine("Usage: testnote <domain> <note-title> <note-content>");
            Console.WriteLine("Example: testnote example-vc.com \"Meeting Notes\" \"Discussion about investment thesis\"");
            return;
        }

        string domain = args[0];
        string noteTitle = args[1];
        string noteContent = args[2];

        try
        {
            // Step 1: Find the company record in Attio
            Console.WriteLine($"🔍 Looking up company record for {domain}...");
            string? recordId = await AttioHelper.FindAttioRecord(domain);

            if (recordId == null)
            {
                Console.WriteLine($"❌ Could not find Attio record for {domain}");
                return;
            }

            Console.WriteLine($"✅ Found record: {recordId}");

            // Step 2: Create a note attached to the record
            Console.WriteLine($"📝 Creating note '{noteTitle}'...");
            bool success = await AttioHelper.CreateAttioNote(recordId, noteTitle, noteContent, NoteFormat.Plaintext);

            if (success)
            {
                Console.WriteLine($"✅ Successfully created note for {domain}");
            }
            else
            {
                Console.WriteLine($"❌ Failed to create note for {domain}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
    }

    static async Task TestSourcesExtraction(string jsonFilePath)
    {
        try
        {
            Console.WriteLine($"📖 Reading Perplexity JSON from {jsonFilePath}...");

            if (!File.Exists(jsonFilePath))
            {
                Console.WriteLine($"❌ Error: File not found: {jsonFilePath}");
                return;
            }

            string jsonContent = await File.ReadAllTextAsync(jsonFilePath);
            JsonNode? perplexityJson = JsonNode.Parse(jsonContent);

            if (perplexityJson == null)
            {
                Console.WriteLine("❌ Error: Failed to parse JSON file");
                return;
            }

            Console.WriteLine("✅ Successfully parsed JSON");
            Console.WriteLine("\n📝 Extracting sources...\n");

            string sourcesMarkdown = PerplexityHelper.ExtractSourcesAsMarkdown(perplexityJson);

            if (string.IsNullOrEmpty(sourcesMarkdown))
            {
                Console.WriteLine("⚠️  No sources found in the JSON response");
            }
            else
            {
                Console.WriteLine("✅ Successfully extracted sources!\n");
                Console.WriteLine("=== MARKDOWN OUTPUT ===");
                Console.WriteLine(sourcesMarkdown);
                Console.WriteLine("=== END OUTPUT ===");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error testing sources extraction: {ex.Message}");
        }
    }
}
