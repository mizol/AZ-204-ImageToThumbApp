namespace ImageToThumbApp.Features.BlobHandling.Services
{
    public interface IGenerateThumbnail
    {
        Task<MemoryStream> GenerateThumbnailAsync(Stream sourceStream);
    }
}
