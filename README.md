# VCR - Venture Capital Researcher

## Description

VCR is a console application designed to facilitate research on venture capital firms. It integrates with various APIs including Notion, Attio, and Perplexity to gather and manage research data efficiently.

## Environment Setup

Before running the application, ensure that the following environment variables are set with the appropriate API keys:

- `SONAR_API_KEY`: Perplexity API key
- `NOTION_API_KEY`: Notion integration token
- `NOTION_DATABASE_ID`: ID of your Notion investor research database
- `ATTIO_API_KEY`: Attio CRM API key
- `MARK2NOTION_API_KEY`: Mark2Notion API key

Example:
```bash
export SONAR_API_KEY="your_perplexity_key"
export NOTION_API_KEY="your_notion_key"
export NOTION_DATABASE_ID="your_notion_database_id"
export ATTIO_API_KEY="your_attio_key"
export MARK2NOTION_API_KEY="your_mark2notion_key"
```

## First-Time Setup

Before setting environment variables, you need to configure the external services the application depends on.

### 1. Perplexity

1. Sign up at [perplexity.ai](https://www.perplexity.ai) and generate an API key from your account settings.
2. Set it as `SONAR_API_KEY`.

### 2. Notion

**Create the database:**

1. In your Notion workspace, create a new full-page database (not inline).
2. Name it something like "Investor Research".
3. The database must have these two properties — delete the default ones and add:
   - **`Investor Name`** — type: **Title** (this is the default title field; rename it)
   - **`Domain`** — type: **URL**
4. Open the database, click "Share", and copy the database ID from the URL:
   `https://notion.so/your-workspace/<database-id>?v=...`
   The database ID is the 32-character hex string before the `?`.
5. Set it as `NOTION_DATABASE_ID`.

**Create a Notion integration:**

1. Go to [notion.so/my-integrations](https://www.notion.so/my-integrations) and create a new internal integration.
2. Copy the integration token and set it as `NOTION_API_KEY`.
3. Back in your database, click "..." → "Connect to" → select your integration to grant it access.

### 3. Mark2Notion

Mark2Notion converts markdown to Notion blocks. Sign up at [mark2notion.com](https://mark2notion.com), generate an API key, and set it as `MARK2NOTION_API_KEY`.

### 4. Attio

**Get your API key:**

1. In Attio, go to Settings → API → create a new API key with read/write access.
2. Set it as `ATTIO_API_KEY`.

**Add the required custom field:**

The application writes the Notion research URL back to the investor's company record in Attio. You must add a custom field to the Companies object:

1. In Attio, go to Settings → Objects → Companies → Attributes.
2. Add a new attribute:
   - **Name:** `Notion Research URL`
   - **API slug:** `notion_research_url` (must match exactly)
   - **Type:** URL
3. Save the attribute.

**Ensure investor records exist:**

The application looks up each investor by domain in Attio before running research. The company record for the investor domain must already exist in Attio before you run the tool against it.

### 5. Investor Criteria

The repo includes generic template criteria files at `vcr/vcr/investor_criteria.md` and `vcr/vcr/family_office_criteria.md`. Edit these in place, or keep your criteria in a separate private repository and point to them via environment variables:

```bash
export VC_CRITERIA_FILE="/path/to/your/investor_criteria.md"
export FO_CRITERIA_FILE="/path/to/your/family_office_criteria.md"
```

If these variables are not set, the app falls back to the template files in the project directory. The more specific your criteria, the more relevant the Perplexity analysis will be.

### Verify setup

Run the built-in connectivity tests before doing real research:

```bash
dotnet run --test-notion        # confirms Notion integration and database access
dotnet run --ping-attio         # confirms Attio API connectivity
dotnet run --test-query sequoiacap.com  # prints the Perplexity prompt without calling the API
```

## Usage

To run the application, use the following command structure:

```bash
dotnet run <command> <investor-domain>
```

### Commands

-   `<investor-domain>`: Create research for the specified domain (aborts if already exists).
-   `--force-research <investor-domain>`: Create research even if duplicates exist.
-   `--regen-research <investor-domain>`: Delete existing research and create new one.
-   `--test-notion`: Test Notion API connection.
-   `--test-notion-insert`: Test Notion database entry creation with markdown.
-   `--ping-attio`: Ping Attio API for basic connectivity.
-   `--fix-links <domain>`: Update Attio with existing Notion research URL (no new research).
-   `--research-only-no-links <domain>`: Create new research in Notion only (no Attio updates).

### Examples

- Create research:
  ```bash
  dotnet run example-vc.com
  ```

- Force create research:
  ```bash
  dotnet run --force-research example-vc.com
  ```

- Regenerate research:
  ```bash
  dotnet run --regen-research example-vc.com
  ```

## Development Commands

```bash
# Build the project (from vcr/ directory)
dotnet build

# Restore dependencies
dotnet restore

# Clean build artifacts
dotnet clean
```

## API Response Structure

The Perplexity API returns a JSON structure with:
- `id`, `model`, `created` — response metadata
- `usage` — token counts and cost information
- `citations` — array of source URLs
- `search_results` — array of detailed search context
- `choices` — array containing the actual chat response (content extracted from `choices[0].message.content`)

A sample response is saved to `output.json` for reference.

## Error Handling

The application checks for missing environment variables and will not run until all required API keys are configured. It also handles API errors and provides informative messages for troubleshooting.

### Common Issues

- **NETSDK1045**: The installed .NET SDK version doesn't match the project's target framework. Install the correct SDK version or update `TargetFramework` in `vcr/vcr.csproj`.
- **Missing API Key**: Ensure all required environment variables are set before running.
- **Network Issues**: Check internet connectivity and API endpoint availability.

## Dependencies

- **System.Net.Http**: For making HTTP requests to external APIs.
- **System.Text.Json**: For JSON parsing and serialization.
- **vcrutils**: Custom utilities for handling API interactions and data processing.

## Contributing

Contributions are welcome! Please fork the repository and submit a pull request with your changes.

## License

This project is licensed under the MIT License.
