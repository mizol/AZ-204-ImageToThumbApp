# ImageToThumbApp

## Project Goal
This project is a lab exercise designed to learn the implementation of **Azure Functions** with an **EventGrid Trigger**. The main objective is to:

- Process image files uploaded to Azure Blob Storage.
- Generate thumbnails for the images and save them back to Blob Storage.
- Understand the integration of Azure Event Grid for event-driven solutions.

This project emphasizes learning best practices for scalable and maintainable Azure Functions while adhering to enterprise-grade architecture principles.

---

## Project Structure
The project follows a structure adhering to the **Vertical Slice Architecture (VSA)** to keep features isolated and modular:

```
ImageToThumbApp
    Extensions
        ServiceCollectionExtensions.cs        // Extension methods for DI setup
    ImageProcessing
        GenerateThumbnailService.cs          // Service for generating thumbnails
        IGenerateThumbnail.cs                // Interface for the thumbnail service
    Features
        BlobHandling
            Events
                BlobCreatedEventData.cs      // Model for handling EventGrid blob events
    host.json                                 // Azure Function runtime settings
    ImageThumbnailFunction.cs                // Main Azure Function class
    local.settings.json                       // Local configuration for development
    Program.cs                                // Entry point and service configuration
    sample-event.json                         // Example EventGrid event payload
```

---

## Lab Steps for Project Implementation

### Step 1: Install Required NuGet Packages
Install the following NuGet packages to add the necessary dependencies:

```bash
# Install Azure dependencies
 dotnet add package Azure.Identity
 dotnet add package Azure.Storage.Blobs
 dotnet add package Microsoft.Azure.Functions.Worker
 dotnet add package Microsoft.Extensions.Configuration.Json

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

### Step 5: Run and Test the Application
- Upload an image to the blob storage's `originals` container.
- Verify that a thumbnail is generated and saved to the `thumbnails` container.

### Step 6: Debug and Monitor
Use Azure Monitor to check the logs and metrics for the function app.

---

## Helpful Resources
- [Azure Functions Documentation](https://learn.microsoft.com/en-us/azure/azure-functions/)
- [Azure Event Grid Documentation](https://learn.microsoft.com/en-us/azure/event-grid/)
- [Quickstart: Create an Event Grid trigger for Azure Functions](https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-event-grid-trigger?tabs=dotnet)
- [Quickstart: Blob Storage and Event Grid](https://learn.microsoft.com/en-us/azure/event-grid/event-grid-event-handlers#blob-storage)

---

Keep learning!

