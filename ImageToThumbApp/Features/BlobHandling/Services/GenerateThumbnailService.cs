using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace ImageToThumbApp.Features.BlobHandling.Services
{
    public class GenerateThumbnailService : IGenerateThumbnail
    {
        private readonly int _maxWidth;
        private readonly int _maxHeight;

        public GenerateThumbnailService(int maxWidth, int maxHeight)
        {
            if (maxWidth <= 0 || maxHeight <= 0)
            {
                throw new ArgumentException("Max dimensions must be positive integers.");
            }

            _maxWidth = maxWidth;
            _maxHeight = maxHeight;
        }

        public async Task<MemoryStream> GenerateThumbnailAsync(Stream sourceStream)
        {
            if (sourceStream == null || sourceStream.Length == 0)
            {
                throw new ArgumentException("Source stream cannot be null or empty.");
            }

            using var image = await Image.LoadAsync(sourceStream);

            var (newWidth, newHeight) = CalculateDimensions(image.Width, image.Height);

            // Resize the image while maintaining aspect ratio
            image.Mutate(x => x.Resize(newWidth, newHeight, KnownResamplers.Lanczos3));

            // Save the resized image to a new stream
            var thumbnailStream = new MemoryStream();
            await image.SaveAsPngAsync(thumbnailStream);
            thumbnailStream.Position = 0;

            return thumbnailStream;
        }

        public async Task<MemoryStream> GenerateThumbnailAsyncV2(Stream sourceStream)
        {
            if (sourceStream == null || sourceStream.Length == 0)
            {
                throw new ArgumentException("Source stream cannot be null or empty.");
            }

            using var image = await Image.LoadAsync(sourceStream);

            // Resize logic here
            int width = Math.Min(_maxWidth, image.Width);
            int height = Math.Min(_maxHeight, image.Height);
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(width, height)
            }));

            // Save the resized image to a new stream
            var thumbnailStream = new MemoryStream();
            await image.SaveAsPngAsync(thumbnailStream);
            thumbnailStream.Position = 0;

            return thumbnailStream;
        }

        private (int newWidth, int newHeight) CalculateDimensions(int originalWidth, int originalHeight)
        {
            double aspectRatio = (double)originalWidth / originalHeight;

            int newWidth, newHeight;
            if (originalWidth > originalHeight) // Landscape
            {
                newWidth = _maxWidth;
                newHeight = (int)(_maxWidth / aspectRatio);
            }
            else // Portrait or square
            {
                newHeight = _maxHeight;
                newWidth = (int)(_maxHeight * aspectRatio);
            }

            // Ensure dimensions do not exceed max bounds
            return (Math.Min(newWidth, _maxWidth), Math.Min(newHeight, _maxHeight));
        }
    }
}
