using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Imaging;
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
}
