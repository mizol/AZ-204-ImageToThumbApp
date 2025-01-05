using Azure.Identity;
using Azure.Messaging.EventGrid;
using Azure.Storage.Blobs;
using ImageToThumbApp;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

public class ImageThumbnailFunction
{
    private readonly ILogger _logger;
    private readonly BlobServiceClient _blobServiceClient;

    public ImageThumbnailFunction(ILoggerFactory loggerFactory, BlobServiceClient blobServiceClient)
    {
        _logger = loggerFactory.CreateLogger<ImageThumbnailFunction>();
        _blobServiceClient = blobServiceClient;
    }

    [Function("ImageThumbnailFunction")]
    public async Task RunAsync(
        [EventGridTrigger] Azure.Messaging.EventGrid.EventGridEvent eventGridEvent)
    {
        _logger.LogInformation("Received Event Grid event subject: {Subject}", eventGridEvent.Subject);

        try
        {
            switch (eventGridEvent.EventType)
            {
                case "Microsoft.Storage.BlobCreated":
                    var blobCreatedData = eventGridEvent.Data.ToObjectFromJson<BlobCreatedEventData>();

                    // Handle blob created event
                    await HandleBlobFile(blobCreatedData);
                    break;

                // Handle other event types...
                default:
                    _logger.LogWarning("Unhandled event type: {EventType}", eventGridEvent.EventType);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "An error occurred while processing the image: {ErrorMessage}", ex.Message);
        }
    }

    private async Task HandleBlobFile(BlobCreatedEventData blobCreatedEventData)
    {
        if (blobCreatedEventData is null)
        {
            _logger.LogWarning("Blob data is null");
            return;
        }

        string? blobUrl = blobCreatedEventData?.Url;
        if (string.IsNullOrEmpty(blobUrl))
        {
            _logger.LogWarning("Blob URL is null");
            return;
        }

        _logger.LogInformation("Blob URL: {BlobUrl}", blobUrl);

        // https://store4azurefunclearn.blob.core.windows.net/originals/2024_05_10_MothersDay.jpg

        //var blobName = Path.GetFileName(blobUrl);
        //BlobClient blobClient = _blobServiceClient.GetBlobContainerClient("originals").GetBlobClient(blobName);

        var sourceBlobClient = new BlobClient(new Uri(blobUrl), new DefaultAzureCredential());

        using var sourceStream = new MemoryStream();
        await sourceBlobClient.DownloadToAsync(sourceStream);
        sourceStream.Position = 0;

        using var image = Image.Load(sourceStream);

        int width = image.Width / 2;
        int height = image.Height / 2;
        image.Mutate(x => x.Resize(width, height, KnownResamplers.Lanczos3));

        // Prepare the destination blob client
        //var thumbnailBlobName = string.Concat(Path.GetFileNameWithoutExtension(blobUrl), ".png");
        
        var thumbnailBlobUrl = blobUrl.Replace("originals", "thumbnails");
        var destinationBlobClient = new BlobClient(new Uri(thumbnailBlobUrl), new DefaultAzureCredential());

        using var thumbnailStream = new MemoryStream();
        await image.SaveAsPngAsync(thumbnailStream);
        thumbnailStream.Position = 0;

        // Upload the thumbnail to the destination container
        await destinationBlobClient.UploadAsync(thumbnailStream, overwrite: true);

        _logger.LogInformation("Thumbnail created and uploaded successfully.");
    }
}
