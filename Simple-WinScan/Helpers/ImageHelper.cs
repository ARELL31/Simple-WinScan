using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Imaging;
using Simple_WinScan.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;
using Windows.Storage.Streams;

namespace Simple_WinScan.Helpers;

public static class ImageHelper
{
    public static async Task<BitmapImage?> CreateBitmapImageAsync(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0) return null;

        try
        {
            using var stream = new InMemoryRandomAccessStream();
            using (var writer = new DataWriter(stream.GetOutputStreamAt(0)))
            {
                writer.WriteBytes(bytes);
                await writer.StoreAsync();
                await writer.FlushAsync();
            }

            stream.Seek(0);
            var bitmapImage = new BitmapImage();
            await bitmapImage.SetSourceAsync(stream);
            return bitmapImage;
        }
        catch
        {
            return null;
        }
    }

    public static byte[] ProcessImage(byte[] rawBytes, int rotationAngle, CropRectangle? crop = null, IImageEncoder? encoder = null)
    {
        if (rawBytes == null || rawBytes.Length == 0) return Array.Empty<byte>();

        int rotation = (rotationAngle % 360 + 360) % 360;
        bool hasCrop = crop != null && !crop.IsFullImage;

        if (rotation == 0 && !hasCrop && encoder == null)
        {
            return rawBytes;
        }

        using var image = Image.Load(rawBytes);

        if (rotation != 0)
        {
            switch (rotation)
            {
                case 90:
                    image.Mutate(x => x.Rotate(90));
                    break;
                case 180:
                    image.Mutate(x => x.Rotate(180));
                    break;
                case 270:
                    image.Mutate(x => x.Rotate(270));
                    break;
            }
        }

        if (hasCrop && crop != null)
        {
            int cropX = (int)Math.Round(crop.X * image.Width);
            int cropY = (int)Math.Round(crop.Y * image.Height);
            int cropW = (int)Math.Round(crop.Width * image.Width);
            int cropH = (int)Math.Round(crop.Height * image.Height);

            cropX = Math.Clamp(cropX, 0, image.Width - 1);
            cropY = Math.Clamp(cropY, 0, image.Height - 1);
            cropW = Math.Clamp(cropW, 1, image.Width - cropX);
            cropH = Math.Clamp(cropH, 1, image.Height - cropY);

            var rect = new Rectangle(cropX, cropY, cropW, cropH);
            image.Mutate(x => x.Crop(rect));
        }

        using var ms = new MemoryStream();
        if (encoder != null)
        {
            image.Save(ms, encoder);
        }
        else
        {
            image.SaveAsPng(ms);
        }
        return ms.ToArray();
    }
}
