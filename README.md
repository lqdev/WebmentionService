# Webmention Service

An Azure Functions-based service for receiving, validating, and processing [Webmentions](https://www.w3.org/TR/webmention/) written in F#. This service accepts webmention notifications, validates them, stores them in Azure Table Storage, and generates RSS feeds from the collected mentions.

**Live Service**: `https://webmentions.lqdev.tech/api/inbox`

## What are Webmentions?

Webmentions are a web standard for notifications between websites. When someone mentions your website on their blog, social media, or any other web platform, a webmention allows your site to be automatically notified about the mention. This enables decentralized social interactions across the web.

## Features

- **HTTP Endpoint**: Accepts webmention requests via POST to `/inbox`
- **Validation**: Validates webmentions using the [WebmentionFs](https://www.nuget.org/packages/lqdev.WebmentionFs) library
- **Storage**: Persists validated webmentions in Azure Table Storage
- **RSS Generation**: Automatically generates RSS feeds from stored webmentions
- **Mention Types**: Supports different types of mentions (likes, replies, reposts, bookmarks)

## Architecture

The service consists of two main Azure Functions:

### ReceiveWebmention Function
- **Trigger**: HTTP POST requests to `/inbox`
- **Purpose**: Receives and validates incoming webmention requests
- **Process**:
  1. Validates the webmention request format and source/target URLs
  2. Verifies that the source URL actually mentions the target URL
  3. Stores valid webmentions in Azure Table Storage
  4. Returns appropriate HTTP responses (200 for success, 400 for errors)

### WebmentionToRss Function
- **Trigger**: Timer-based (runs daily at 3 AM UTC: `0 0 3 * * *`)
- **Purpose**: Generates RSS feeds from stored webmentions
- **Process**:
  1. Retrieves webmentions from Azure Table Storage
  2. Builds an RSS feed containing recent mentions
  3. Saves the RSS feed to Azure Blob Storage

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Azure Functions Core Tools](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local)
- Azure Storage Account (for Table Storage and Blob Storage)

## Configuration

The service requires the following environment variables:

### Required Settings

| Variable | Description | Example |
|----------|-------------|---------|
| `AzureWebJobsStorage` | Azure Storage connection string | `DefaultEndpointsProtocol=https;AccountName=...` |
| `PERSONAL_WEBSITE_HOSTNAMES` | Comma-separated list of hostnames to accept webmentions for | `example.com,www.example.com` |

### Azure Resources

- **Azure Storage Account**: Used for both Table Storage (webmentions) and Blob Storage (RSS feeds)
- **Azure Functions App**: Hosts the webmention processing functions

## Local Development

1. **Clone the repository**:
   ```bash
   git clone https://github.com/lqdev/WebmentionService.git
   cd WebmentionService
   ```

2. **Install dependencies**:
   ```bash
   dotnet restore
   ```

3. **Configure local settings**:
   Create a `local.settings.json` file:
   ```json
   {
     "IsEncrypted": false,
     "Values": {
       "AzureWebJobsStorage": "UseDevelopmentStorage=true",
       "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
       "PERSONAL_WEBSITE_HOSTNAMES": "localhost:3000,127.0.0.1:3000"
     }
   }
   ```

4. **Start Azure Storage Emulator** (for local development):
   ```bash
   azurite --silent --location c:\azurite --debug c:\azurite\debug.log
   ```

5. **Run the function app**:
   ```bash
   func start
   ```

## Deployment

### Azure Deployment

1. **Create Azure Resources**:
   - Azure Functions App (with .NET 6.0 runtime)
   - Azure Storage Account

2. **Configure Application Settings** in your Azure Functions App:
   - `AzureWebJobsStorage`: Your Azure Storage connection string
   - `PERSONAL_WEBSITE_HOSTNAMES`: Your website hostnames

3. **Deploy using Azure Functions Core Tools**:
   ```bash
   func azure functionapp publish <your-function-app-name>
   ```

### Alternative Deployment Methods

- [GitHub Actions](https://docs.microsoft.com/en-us/azure/azure-functions/functions-how-to-github-actions)
- [Azure DevOps](https://docs.microsoft.com/en-us/azure/azure-functions/functions-how-to-azure-devops)
- [Visual Studio](https://docs.microsoft.com/en-us/azure/azure-functions/functions-develop-vs)

## API Usage

### Send a Webmention

Send a POST request to the `/inbox` endpoint:

```bash
curl -X POST https://your-function-app.azurewebsites.net/api/inbox \
     -H "Content-Type: application/x-www-form-urlencoded" \
     -d "source=https://example.com/post&target=https://yourdomain.com/article"
```

**Parameters**:
- `source` (required): URL of the page that mentions your content
- `target` (required): URL of your content being mentioned

**Responses**:
- `200 OK`: Webmention processed successfully
- `400 Bad Request`: Invalid webmention or validation error

### RSS Feed

The generated RSS feed will be available at:
```
https://your-storage-account.blob.core.windows.net/feeds/webmentions/index.xml
```

## Data Storage

### Table Storage Schema

Webmentions are stored in the `webmentions` table with the following structure:

| Field | Type | Description |
|-------|------|-------------|
| `PartitionKey` | string | URL-encoded target URL |
| `RowKey` | string | URL-encoded source URL |
| `IsBookmark` | boolean | Whether this is a bookmark mention |
| `IsLike` | boolean | Whether this is a like mention |
| `IsReply` | boolean | Whether this is a reply mention |
| `IsRepost` | boolean | Whether this is a repost mention |
| `Timestamp` | DateTimeOffset | When the webmention was received |

## Dependencies

This service is built on top of several key libraries:

- **[WebmentionFs](https://www.nuget.org/packages/lqdev.WebmentionFs)** (v0.0.7): Core webmention validation and processing
- **Microsoft.Azure.Functions.Extensions**: Azure Functions dependency injection
- **Microsoft.Azure.WebJobs.Extensions.Tables**: Azure Table Storage bindings
- **Microsoft.Azure.WebJobs.Extensions.Storage**: Azure Storage bindings

## Troubleshooting

### Common Issues

1. **"Webmention already exists" error**:
   - This occurs when the same source/target pair is submitted multiple times
   - The service prevents duplicate webmentions by using the source/target URLs as the primary key

2. **Validation errors**:
   - Ensure the source URL actually contains a link to the target URL
   - Check that both URLs are valid and accessible
   - Verify that the target hostname is listed in `PERSONAL_WEBSITE_HOSTNAMES`

3. **Storage connection issues**:
   - Verify the `AzureWebJobsStorage` connection string is correct
   - Ensure the storage account has the necessary permissions
   - For local development, make sure the Azure Storage Emulator is running

### Monitoring

- Check Azure Functions logs in the Azure portal
- Monitor Table Storage for stored webmentions
- Verify RSS feed generation in Blob Storage

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.

## License

This project is licensed under the terms specified in the repository.