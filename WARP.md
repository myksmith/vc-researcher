# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Project Overview

This is a simple C# console application called "vc-researcher" that demonstrates integration with Perplexity AI's Sonar API. The application makes HTTP requests to the Perplexity API to query information and processes the JSON responses to extract chat completions.

## Architecture

### Structure
```
vcr/                          # Main solution directory
├── vcr.sln                  # Visual Studio solution file
└── vcr/                     # Console application project
    ├── vcr.csproj          # Project file (.NET 9.0 target)
    ├── Program.cs          # Main application entry point
    └── output.json         # Sample API response output
```

### Key Components
- **Program.cs**: Single-file console application that:
  - Configures HttpClient with Perplexity API authentication
  - Reads investor criteria from `Neo_Investor_Search_Criteria.md`
  - Sends POST requests to `https://api.perplexity.ai/chat/completions`
  - Uses the `sonar-pro` model for queries
  - Generates detailed queries with investor criteria context
  - Parses JSON responses and extracts chat content
  - Handles errors and displays both full response and extracted content
- **Neo_Investor_Search_Criteria.md**: Contains detailed investor search criteria for Neo's $5M seed round

### Dependencies
- **.NET 9.0**: Target framework (note: requires .NET 9.0 SDK)
- **System.Net.Http**: For HTTP client functionality
- **System.Text.Json**: For JSON serialization/deserialization
- **System.Text.Json.Nodes**: For JSON parsing and navigation
- **System.IO**: For file operations to read investor criteria

## Environment Setup

### Prerequisites
- .NET 9.0 SDK (the project targets .NET 9.0, not compatible with .NET 8.0)
- All required environment variables must be set (see API Key Configuration section)

### API Key Configuration
The application requires multiple environment variables for different services and workspace configuration:

#### API Keys
- `SONAR_API_KEY`: Perplexity API key
- `NOTION_API_KEY`: Notion API key
- `ATTIO_API_KEY`: Attio CRM API key
- `MARK2NOTION_API_KEY`: Mark2Notion API key

#### Workspace Configuration
- `NOTION_DATABASE_ID`: The ID of your Notion database for investor research
- `NOTION_DATABASE_NAME`: The name of your Notion database (used for searches)
- `ATTIO_PRESEED_LIST_NAME`: The name of your Attio list for preseed VCs
- `ATTIO_STARTUP_LIST_NAME`: The name of your Attio list for startup fundraising

Example configuration:
```bash
export SONAR_API_KEY="your_perplexity_key"
export NOTION_API_KEY="your_notion_key"
export ATTIO_API_KEY="your_attio_key"
export MARK2NOTION_API_KEY="your_mark2notion_key"
export NOTION_DATABASE_ID="your_notion_database_id"
export NOTION_DATABASE_NAME="your_database_name"
export ATTIO_PRESEED_LIST_NAME="your_preseed_list_name"
export ATTIO_STARTUP_LIST_NAME="your_startup_list_name"
```

**Note**: These configuration values are workspace-specific. When setting up the application for your own use, you'll need to configure these environment variables to match your specific Notion workspace and Attio CRM setup.

## Development Commands

### Building the Project
```bash
# From the vcr/ directory (solution root)
dotnet build

# Or build the specific project
dotnet build vcr/vcr.csproj
```

### Running the Application
```bash
# From the vcr/ directory
dotnet run --project vcr <investor-domain>

# Or from vcr/vcr/ directory
dotnet run <investor-domain>

# Example usage
dotnet run --project vcr sequoiacap.com
```

### Cleaning Build Artifacts
```bash
# From the vcr/ directory
dotnet clean
```

### Restoring Dependencies
```bash
# From the vcr/ directory
dotnet restore
```

## Working with the Code

### Command-Line Usage
The application now accepts a single command-line argument for the investor domain to research:
```bash
dotnet run <investor-domain>
```

The application reads investor criteria from `Neo_Investor_Search_Criteria.md` and generates a comprehensive query that includes:
- The investor domain to research
- Full investor criteria context from the markdown file
- Specific analysis points including stage/check size match, portfolio alignment, geographic fit, and overall recommendation

### Modifying the Query Template
To change the query template, modify lines 60-68 in `Program.cs`:
```csharp
content = $"Your new query template with {investorDomain} and {investorCriteria}"
```

### Modifying Investor Criteria
To update the investor criteria, edit the `Neo_Investor_Search_Criteria.md` file directly. The application will automatically read and include the updated criteria in all queries.

### Changing the AI Model
To use a different Perplexity model, modify line 21 in `Program.cs`:
```csharp
model = "sonar-reasoning"  // or other available models
```

### API Response Structure
The API returns a complex JSON structure with:
- `id`, `model`, `created` metadata
- `usage` information including token counts and costs
- `citations` array with source URLs
- `search_results` array with detailed search context
- `choices` array containing the actual chat response

The application extracts the main response from `choices[0].message.content`.

## Important Notes

- **SDK Version**: This project requires .NET 9.0 SDK. If you're using .NET 8.0, you'll need to either upgrade your SDK or modify the `TargetFramework` in `vcr.csproj` to `net8.0`
- **API Key Security**: All API keys are read from environment variables. Never commit API keys to the repository
- **Workspace Configuration**: The workspace-specific configuration values (database IDs, list names) are now configurable via environment variables, making the application suitable for open source distribution
- **Rate Limits**: Be aware that the Perplexity API has rate limits and costs associated with usage
- **Error Handling**: The application includes basic error handling but may need enhancement for production use

## Debugging

### Common Issues
1. **Build Error NETSDK1045**: Indicates .NET 9.0 SDK is not installed. Either install .NET 9.0 SDK or downgrade the target framework
2. **Missing API Keys or Configuration**: Ensure all required environment variables are set (the application will check and report missing variables on startup)
3. **Network Issues**: Check internet connection and API endpoint availability
4. **Workspace Configuration**: Ensure your Notion database ID, database name, and Attio list names are correctly configured in the environment variables

### Output Files
- `output.json`: Contains a sample API response for reference
- Build outputs are in `vcr/bin/` and `vcr/obj/` directories (git-ignored)