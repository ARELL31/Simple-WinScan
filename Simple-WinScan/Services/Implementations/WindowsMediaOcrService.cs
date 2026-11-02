using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Simple_WinScan.Services.Interfaces;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace Simple_WinScan.Services.Implementations;

public class WindowsMediaOcrService : IOcrService
{
    private readonly OcrEngine? _engine;

    public bool IsOcrSupported => _engine != null;

    public WindowsMediaOcrService()
    {
        try
        {
            _engine = OcrEngine.TryCreateFromUserProfileLanguages();
        }
        catch
        {
            _engine = null;
        }
    }

    public async Task<OcrPageResult?> RecognizeTextAsync(byte[] imageBytes, CancellationToken cancellationToken = default)
    {
        if (_engine == null || imageBytes == null || imageBytes.Length == 0)
        {
            return null;
        }

        try
        {
            using var stream = new InMemoryRandomAccessStream();
            using (var writer = new DataWriter(stream))
            {
                writer.WriteBytes(imageBytes);
                await writer.StoreAsync();
                await writer.FlushAsync();
                writer.DetachStream();
            }
            stream.Seek(0);

            var decoder = await BitmapDecoder.CreateAsync(stream);
            var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

            cancellationToken.ThrowIfCancellationRequested();

            var ocrResult = await _engine.RecognizeAsync(softwareBitmap);
            if (ocrResult == null) return null;

            var result = new OcrPageResult
            {
                FullText = ocrResult.Text
            };

            foreach (var line in ocrResult.Lines)
            {
                foreach (var word in line.Words)
                {
                    result.Words.Add(new OcrWordBox
                    {
                        Text = word.Text,
                        X = word.BoundingRect.X,
                        Y = word.BoundingRect.Y,
                        Width = word.BoundingRect.Width,
                        Height = word.BoundingRect.Height
                    });
                }
            }

            return result;
        }
        catch
        {
            return null;
        }
    }
}
