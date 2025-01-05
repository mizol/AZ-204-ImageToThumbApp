using Azure.Messaging.EventGrid;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace ImageToThumbApp
{
    public class ImageThumbnailFunction
    {
        private const string EventTypeBlobCreated = "Microsoft.Storage.BlobCreated";
        private readonly ILogger _logger;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _originalsFolder;
        private readonly string _thumbnailsFolder;

        public ImageThumbnailFunction(ILoggerFactory loggerFactory, BlobServiceClient blobServiceClient, IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<ImageThumbnailFunction>();
            _blobServiceClient = blobServiceClient;
            _originalsFolder = configuration["BlobFolders:Originals"]!;
            _thumbnailsFolder = configuration["BlobFolders:Thumbnails"]!;
        }

        [Function("ImageThumbnailFunction")]
        public async Task RunAsync(
            [EventGridTrigger] EventGridEvent eventGridEvent)
        {
            _logger.LogInformation("Received Event Grid event subject: {Subject}", eventGridEvent.Subject);

            try
            {
                if (eventGridEvent.EventType != EventTypeBlobCreated)
                {
                    _logger.LogWarning("Unhandled event type: {EventType}", eventGridEvent.EventType);
                    return;
                }

                var blobCreatedData = eventGridEvent.Data.ToObjectFromJson<BlobCreatedEventData>();
                if (blobCreatedData is null)
                {
                    _logger.LogWarning("Can't parse bynary data to {0}", nameof(BlobCreatedEventData));
                    return;
                }

                await HandleBlobFile(blobCreatedData);

            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while processing the image: {ErrorMessage}", ex.Message);
            }
        }

        private async Task HandleBlobFile(BlobCreatedEventData blobCreatedEventData)
        {
            string? blobUrl = blobCreatedEventData.Url;
            if (string.IsNullOrEmpty(blobUrl))
            {
                _logger.LogWarning("Blob URL is null");
                return;
            }

            _logger.LogInformation("Blob created URL: {BlobUrl}", blobUrl);

            // Download image
            var sourceBlobClient = GetBlobClient(blobUrl, _originalsFolder);
            using var sourceStream = await DownloadBlobAsync(sourceBlobClient);

            // Process image
            using var thumbnailStream = await MutateImageToPngThumbAsync(sourceStream);

            // Upload thumbnail
            var thumbnailBlobUrl = ReplaceExtension(blobUrl.Replace(_originalsFolder, _thumbnailsFolder), ".png");
            var destinationBlobClient = GetBlobClient(thumbnailBlobUrl, _thumbnailsFolder);

            await UploadBlobAsync(destinationBlobClient, thumbnailStream);

            _logger.LogInformation("Thumbnail created and uploaded successfully.");
        }

        private string ReplaceExtension(string fileName, string newExtension)
        {
            var lastDotIndex = fileName.LastIndexOf('.');
            return lastDotIndex > 0 
                ? fileName.Substring(0, lastDotIndex) + newExtension 
                : fileName + newExtension;
        }

        private BlobClient GetBlobClient(string blobUrl, string containerFolder)
        {
            var uri = new Uri(blobUrl);
            return _blobServiceClient.GetBlobContainerClient(containerFolder)
                                      .GetBlobClient(uri.Segments[^1]);
        }

        private async Task<MemoryStream> DownloadBlobAsync(BlobClient blobClient)
        {
            var stream = new MemoryStream();
            await blobClient.DownloadToAsync(stream);
            stream.Position = 0;
            return stream;
        }

        private async Task<MemoryStream> MutateImageToPngThumbAsync(MemoryStream sourceStream)
        {
            using var image = await Image.LoadAsync(sourceStream);

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
}