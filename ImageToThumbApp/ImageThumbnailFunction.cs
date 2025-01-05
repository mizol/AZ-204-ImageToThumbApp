using Azure.Identity;
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

                    await HandleBlobFile(blobCreatedData);
                    break;

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

        var sourceBlobClient = GetBlobClient(blobUrl);
        using var sourceStream = await DownloadBlobAsync(sourceBlobClient);
        using var thumbnailStream = await ProcessImageAsync(sourceStream);

        var thumbnailBlobUrl = blobUrl.Replace("originals", "thumbnails");
        var destinationBlobClient = GetBlobClient(thumbnailBlobUrl);

        await UploadBlobAsync(destinationBlobClient, thumbnailStream);

        _logger.LogInformation("Thumbnail created and uploaded successfully.");
    }

    private BlobClient GetBlobClient(string blobUrl)
    {
        var uri = new Uri(blobUrl);
        return _blobServiceClient.GetBlobContainerClient(uri.Segments[1].TrimEnd('/'))
                                  .GetBlobClient(uri.Segments[^1]);
    }

    private async Task<MemoryStream> DownloadBlobAsync(BlobClient blobClient)
    {
        var stream = new MemoryStream();
        await blobClient.DownloadToAsync(stream);
        stream.Position = 0;
        return stream;
    }

    private async Task<MemoryStream> ProcessImageAsync(MemoryStream sourceStream)
    {
        using var image = Image.Load(sourceStream);

        int width = image.Width / 2;
        int height = image.Height / 2;
        image.Mutate(x => x.Resize(width, height, KnownResamplers.Lanczos3));

        var thumbnailStream = new MemoryStream();
        await image.SaveAsPngAsync(thumbnailStream);
        thumbnailStream.Position = 0;

        return thumbnailStream;
    }

    private async Task UploadBlobAsync(BlobClient blobClient, MemoryStream thumbnailStream)
    {
        await blobClient.UploadAsync(thumbnailStream, overwrite: true);
    }
}
