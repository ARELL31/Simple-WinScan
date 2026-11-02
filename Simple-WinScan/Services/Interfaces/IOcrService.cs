using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Simple_WinScan.Services.Interfaces;

public class OcrWordBox
{
    public string Text { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}

public class OcrPageResult
{
    public string FullText { get; set; } = string.Empty;
    public List<OcrWordBox> Words { get; set; } = new();
}

public interface IOcrService
{
    bool IsOcrSupported { get; }
    Task<OcrPageResult?> RecognizeTextAsync(byte[] imageBytes, CancellationToken cancellationToken = default);
}
