using System;

namespace Simple_WinScan.Models;

public class ScannedPage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public byte[] ImageBytes { get; set; } = Array.Empty<byte>();
    public byte[]? OriginalImageBytes { get; set; }
    public int RotationAngle { get; set; } = 0; // 0, 90, 180, 270
    public int PixelWidth { get; set; }
    public int PixelHeight { get; set; }
    public int Dpi { get; set; } = 300;
    public CropRectangle? Crop { get; set; }
    public bool HasCrop => Crop != null && !Crop.IsFullImage;
    public DateTime ScannedAt { get; init; } = DateTime.Now;

    public void RotateClockwise()
    {
        RotationAngle = (RotationAngle + 90) % 360;
    }

    public void RotateCounterClockwise()
    {
        RotationAngle = (RotationAngle - 90 + 360) % 360;
    }

    public ScannedPage Clone()
    {
        byte[] clonedBytes = new byte[ImageBytes.Length];
        Array.Copy(ImageBytes, clonedBytes, ImageBytes.Length);

        byte[]? clonedOrig = null;
        if (OriginalImageBytes != null)
        {
            clonedOrig = new byte[OriginalImageBytes.Length];
            Array.Copy(OriginalImageBytes, clonedOrig, OriginalImageBytes.Length);
        }

        return new ScannedPage
        {
            ImageBytes = clonedBytes,
            OriginalImageBytes = clonedOrig,
            RotationAngle = RotationAngle,
            PixelWidth = PixelWidth,
            PixelHeight = PixelHeight,
            Dpi = Dpi,
            Crop = Crop != null ? new CropRectangle(Crop.X, Crop.Y, Crop.Width, Crop.Height) : null,
            ScannedAt = ScannedAt
        };
    }
}
