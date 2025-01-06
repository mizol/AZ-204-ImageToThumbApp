# ImageToThumbApp

## Project Goal

The goal of this project is to learn and implement Azure Functions using the EventGrid trigger to process blob events in an Azure Storage Account. Specifically, the project demonstrates how to create thumbnail images from uploaded blobs, showcasing cloud-based event-driven architecture and scalability.

The main objective is to:
- Understand the integration of Azure Event Grid for event-driven solutions.
- Process image files uploaded to Azure Blob Storage.
- Generate thumbnails for the images and save them back to Blob Storage.

This project emphasizes learning best practices for scalable and maintainable Azure Functions. 

---

## Project Structure

The project follows a structure adhering to the **Vertical Slice Architecture (VSA)** to keep features isolated and modular:

```
ImageToThumbApp
├── 📁 Properties
│   └── 📄 launchSettings.json
├── 📁 Config
│   ├── 📄 host.json
│   └── 📄 local.settings.json
├── 📁 Features
│   └── 📁 BlobHandling
│       ├── 📁 Events
│       │   └─ 📄 BlobCreatedEventData.cs    // Model for EventGrid blob events
│       ├── 📁 Extensions
│       │   └─ 📄 ServiceCollectionExtensions.cs  // Extension methods for DI setup
│       ├── 📁 Functions
│       │   └─ 📄 ImageThumbnailFunction.cs       // Main Azure Function class
│       └── 📁 Services
│            ├── 📄 GenerateThumbnailService.cs
│            └── 📄 IGenerateThumbnail.cs
├── 📁 Resources
│   └── 📄 sample-event.json
├── 📁 Startup
│   └── 📄 Program.cs    // Entry point and service configuration
├── 📄 .gitignore
└── 📄 Readme.md
```

---

## Lab Steps for Project Implementation

### Step 1: Install Required NuGet Packages
Install the following NuGet packages to add the necessary dependencies:

```bash
# Install Azure dependencies
 dotnet add package Azure.Identity
 dotnet add package Azure.Storage.Blobs
 dotnet add package Azure.Messaging.EventGrid
 dotnet add package Microsoft.Azure.Functions.Worker
 dotnet add package Microsoft.Azure.Functions.Worker.Extensions.EventGrid
 dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore
 dotnet add package Microsoft.Azure.Functions.Worker.Sdk
 dotnet add package Microsoft.Extensions.Azure

# Install ImageSharp for image processing
 dotnet add package SixLabors.ImageSharp
```

### Step 2: Set Up `Program.cs` and Builder Services

1. **Configure Dependency Injection:** Set up services for Blob Storage and the thumbnail generation service.
2. **Read configuration:** Use `local.settings.json` to configure project settings.
3. Use the `ServiceCollectionExtensions` to modularize service registrations.

Example `Program.cs` configuration:

```csharp
var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Add BlobServiceClient and ThumbnailService
builder.Services.AddBlobServiceClient(builder.Configuration);
builder.Services.AddImageProcessingService(builder.Configuration);

builder.Build().Run();
```

Example local.settings.json

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "AzureWebJobsSecretStorageType": "Files",
    "Blob:StorageAccount": "storage_account_name",
    "BlobFolders:Originals": "input_container",
    "BlobFolders:Thumbnails": "output_container",
    "ThumbnailSettings:Width": "150",
    "ThumbnailSettings:Height": "150"
  }
}
```

### Step 3: Create `BlobCreatedEventData` Model

Define a model to deserialize EventGrid payloads for blob creation events. Place this in the `Features/BlobHandling/Events` folder.

Example:

```csharp
public class BlobCreatedEventData
{
    public string Url { get; set; } = string.Empty;
}
```

### Step 4: Configure the Azure Environment
#### Create Resources:
1. **Resource Group:** Create a resource group to host the Azure services:
   ```bash
   az group create --name MyResourceGroup --location eastus
   ```

2. **Storage Account:** Create a storage account for blob operations:
   ```bash
   az storage account create --name mystorageaccount --resource-group MyResourceGroup --location eastus --sku Standard_LRS
   ```

3. **Azure Function App:** Create a function app:
   ```bash
   az functionapp create --resource-group MyResourceGroup --consumption-plan-location eastus --runtime dotnet --name MyFunctionApp --storage-account mystorageaccount
   ```

4. **Managed Identity:** Assign a managed identity to the function app for accessing Blob Storage securely:
   ```bash
   az functionapp identity assign --name MyFunctionApp --resource-group MyResourceGroup
   ```

#### Configure Event Grid:

1. **Event Grid Topic:** Create a custom Event Grid topic or use a storage account's default events.
2. **Event Subscription:** Create a subscription to the Event Grid topic and point it to the Azure Function:
   ```bash
   az eventgrid event-subscription create \
       --name MyEventSubscription \
       --source-resource-id /subscriptions/{subscription-id}/resourceGroups/MyResourceGroup/providers/Microsoft.Storage/storageAccounts/mystorageaccount \
       --endpoint https://{myfunctionapp}.azurewebsites.net/runtime/webhooks/EventGrid?functionName=ImageThumbnailFunction
   ```

### 5. Run and Test the Application

- Upload an image to the blob storage's `originals` container.
- Verify that a thumbnail is generated and saved to the `thumbnails` container.
- Test the Azure Function locally by sending a `BlobCreated` event request:
    - Use the Insomnia app or a similar tool to send the request.
    - The request URL should include the function name as a query parameter, e.g., `http://localhost:7028/runtime/webhooks/EventGrid?functionName=ImageThumbnailFunction`.
    - Set the headers with `aeg-event-type: Notification`.
    - Provide the `BlobCreated` event payload in the request body. The body format is as follows:

```json
[
  {
    "id": "event-id",
    "eventType": "Microsoft.Storage.BlobCreated",
    "topic": "/subscriptions/{subscription-id}/resourceGroups/{resource-group}/providers/Microsoft.Storage/storageAccounts/{storage-account-name}",
  "subject": "/blobServices/default/containers/{container-name}/blobs/{blob-name}",
    "eventTime": "2025-01-05T12:34:56Z",
    "data": {
      "api": "PutBlob",
      "clientRequestId": "client-request-id",
      "requestId": "request-id",
      "eTag": "etag-value",
      "contentType": "image/jpeg",
      "blobType": "BlockBlob",
      "contentLength": 12345,
      "url": "https://{storage-account-name}.blob.core.windows.net/{container-name}/{blob-name}",
    "sequencer": "0000000000000000001"
    },
    "dataVersion": "1.0",
    "metadataVersion": "1"
  }
]
```

- Refer to the `Resources/EventGrid_BlobCreated.png` file for an example of an Insomnia configuration.

### Step 6: Debug and Monitor
- Debug the application locally in Visual Studio:
    - Open the `launchSettings.json` file in the `Properties` folder.
    - Add the `--verbose` flag to the command arguments to enable detailed logging.
    - Monitor the command-line console output logs for debugging information.

```json
{
  "profiles": {
    "ImageToThumbApp": {
      "commandName": "Project",
      "commandLineArgs": "--port 7028 --verbose",
      "launchBrowser": false
    }
  }
}
```
---

## Helpful Resources
- [Azure Functions Documentation](https://learn.microsoft.com/en-us/azure/azure-functions/)
- [Azure Event Grid Documentation](https://learn.microsoft.com/en-us/azure/event-grid/)
- [Quickstart: Create an Event Grid trigger for Azure Functions](https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-event-grid-trigger?tabs=dotnet)
- [Quickstart: Blob Storage and Event Grid](https://learn.microsoft.com/en-us/azure/event-grid/event-grid-event-handlers#blob-storage)

---

Keep learning!