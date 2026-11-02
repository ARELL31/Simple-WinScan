using System;

namespace Simple_WinScan.Models;

public enum CropPreset
{
    Free,
    A4,
    Letter,
    Photo4x6,
    BusinessCard
}

public record CropRectangle
{
    public double X { get; init; }
    public double Y { get; init; }
    public double Width { get; init; }
    public double Height { get; init; }

    public bool IsFullImage =>
        Math.Abs(X) < 0.001 &&
        Math.Abs(Y) < 0.001 &&
        Math.Abs(Width - 1.0) < 0.001 &&
        Math.Abs(Height - 1.0) < 0.001;

    public CropRectangle(double x, double y, double width, double height)
    {
        double clampedX = Math.Clamp(x, 0.0, 1.0);
        double clampedY = Math.Clamp(y, 0.0, 1.0);
        double clampedW = Math.Clamp(width, 0.05, 1.0 - clampedX);
        double clampedH = Math.Clamp(height, 0.05, 1.0 - clampedY);

        X = clampedX;
        Y = clampedY;
        Width = clampedW;
        Height = clampedH;
    }

    public static CropRectangle Full => new(0.0, 0.0, 1.0, 1.0);

    public CropRectangle Move(double deltaX, double deltaY)
    {
        double newX = Math.Clamp(X + deltaX, 0.0, 1.0 - Width);
        double newY = Math.Clamp(Y + deltaY, 0.0, 1.0 - Height);
        return new CropRectangle(newX, newY, Width, Height);
    }

    public CropRectangle Resize(double deltaW, double deltaH)
    {
        double newW = Math.Clamp(Width + deltaW, 0.05, 1.0 - X);
        double newH = Math.Clamp(Height + deltaH, 0.05, 1.0 - Y);
        return new CropRectangle(X, Y, newW, newH);
    }

    public static CropRectangle CreatePreset(CropPreset preset, double pageAspectRatio)
    {
        if (pageAspectRatio <= 0) pageAspectRatio = 1.0;

        double targetRatio = preset switch
        {
            CropPreset.A4 => 210.0 / 297.0,
            CropPreset.Letter => 8.5 / 11.0,
            CropPreset.Photo4x6 => 4.0 / 6.0,
            CropPreset.BusinessCard => 85.6 / 53.98,
            _ => pageAspectRatio
        };

        double cropW = 0.85;
        double cropH = cropW * (pageAspectRatio / targetRatio);

        if (cropH > 0.85)
        {
            cropH = 0.85;
            cropW = cropH * (targetRatio / pageAspectRatio);
        }

        cropW = Math.Clamp(cropW, 0.1, 0.95);
        cropH = Math.Clamp(cropH, 0.1, 0.95);

        double cropX = (1.0 - cropW) / 2.0;
        double cropY = (1.0 - cropH) / 2.0;

        return new CropRectangle(cropX, cropY, cropW, cropH);
    }
}
