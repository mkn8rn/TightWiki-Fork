using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace TightWiki.Helpers
{
    public static class WebUtility
    {
        public static byte[] ConvertHttpFileToBytes(IFormFile image)
        {
            using var stream = image.OpenReadStream();
            using BinaryReader reader = new BinaryReader(stream);
            return reader.ReadBytes((int)image.Length);
        }

        /// <summary>
        /// Crops an image to a centered square and returns the result as a byte array.
        /// </summary>
        public static byte[] CropImageToCenteredSquare(MemoryStream inputStream)
        {
            using var image = Image.Load(inputStream);

            if (image.Width != image.Height)
            {
                int size = Math.Min(image.Width, image.Height);
                int x = (image.Width - size) / 2;
                int y = (image.Height - size) / 2;

                image.Mutate(ctx => ctx.Crop(new Rectangle(x, y, size, size)));
            }

            using var outputStream = new MemoryStream();
            image.SaveAsWebp(outputStream);
            return outputStream.ToArray();
        }
    }
}
