using System;

namespace Simple_WinScan.Models;

public class ScannedPage
{
    public Guid Id { get; } = Guid.NewGuid();
    public byte[] ImageBytes { get; set; } = Array.Empty<byte>();
    public int RotationAngle { get; set; } = 0; // 0, 90, 180, 270
    public int PixelWidth { get; set; }
    public int PixelHeight { get; set; }
    public int Dpi { get; set; } = 300;
    public DateTime ScannedAt { get; } = DateTime.Now;

    public void RotateClockwise()
    {
        RotationAngle = (RotationAngle + 90) % 360;
    }

    public void RotateCounterClockwise()
    {
        RotationAngle = (RotationAngle - 90 + 360) % 360;
    }
}
