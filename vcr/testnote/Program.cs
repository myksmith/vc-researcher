using System;
using System.Threading.Tasks;
using vcrutils;

class Program
{
    static async Task Main(string[] args)
    {
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
}
