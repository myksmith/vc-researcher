# VCR - Venture Capital Researcher

## Description

VCR is a console application designed to facilitate research on venture capital firms. It integrates with various APIs including Notion, Attio, and Perplexity to gather and manage research data efficiently.

## Environment Setup

Before running the application, ensure that the following environment variables are set with the appropriate API keys and workspace configuration:

### API Keys
- `SONAR_API_KEY`: Perplexity API
- `NOTION_API_KEY`: Notion API
- `ATTIO_API_KEY`: Attio CRM API
- `MARK2NOTION_API_KEY`: Mark2Notion API

### Workspace Configuration
- `NOTION_DATABASE_ID`: The ID of your Notion database for investor research
- `NOTION_DATABASE_NAME`: The name of your Notion database (used for searches)
- `ATTIO_PRESEED_LIST_NAME`: The name of your Attio list for preseed VCs
- `ATTIO_STARTUP_LIST_NAME`: The name of your Attio list for startup fundraising

Example:
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

### Finding Your Configuration Values

- **NOTION_DATABASE_ID**: Copy the database ID from your Notion database URL (the long string after the last slash and before any query parameters)
- **NOTION_DATABASE_NAME**: The exact name of your investor research database in Notion
- **ATTIO_PRESEED_LIST_NAME**: The name of your Attio list containing preseed VCs
- **ATTIO_STARTUP_LIST_NAME**: The name of your Attio list for startup fundraising contacts

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
-   `--test-attio-list`: Test Attio list lookup for both target databases.
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

## Error Handling

The application checks for missing environment variables and will not run until all required API keys and configuration are set. It also handles API errors and provides informative messages for troubleshooting.

## Dependencies

- **System.Net.Http**: For making HTTP requests to external APIs.
- **System.Text.Json**: For JSON parsing and serialization.
- **vcrutils**: Custom utilities for handling API interactions and data processing.

## Contributing

Contributions are welcome! Please fork the repository and submit a pull request with your changes.

## License

This project is licensed under the MIT License.
