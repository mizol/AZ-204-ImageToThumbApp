using Azure.Identity;
using Azure.Storage.Blobs;
using ImageToThumbApp.Features.BlobHandling.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ImageToThumbApp.Features.BlobHandling.Extentions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBlobServiceClient(this IServiceCollection services, IConfiguration configuration)
        {
            var storageAccountSettings = configuration.GetRequiredSection("Blob");
            string blobAccount = storageAccountSettings["StorageAccount"] ??
                throw new InvalidOperationException("Blob storage account name is missing.");

            services.AddSingleton(_ =>
                new BlobServiceClient(
                    new Uri($"https://{blobAccount}.blob.core.windows.net"),
                    new DefaultAzureCredential()));

            return services;
        }

        public static IServiceCollection AddImageThumbnailService(this IServiceCollection services, IConfiguration configuration)
        {
            var thumbnailSettings = configuration.GetSection("ThumbnailSettings");
            int width = int.Parse(thumbnailSettings["Width"] ?? "150");
            int height = int.Parse(thumbnailSettings["Height"] ?? "150");

            services.AddSingleton<IGenerateThumbnail>(_ =>
                new GenerateThumbnailService(maxWidth: width, maxHeight: height));

            return services;
        }
    }

}
