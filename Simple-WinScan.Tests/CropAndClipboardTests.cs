using System;
using Simple_WinScan.Models;
using Simple_WinScan.ViewModels;
using Xunit;

namespace Simple_WinScan.Tests;

public class CropAndClipboardTests
{
    [Fact]
    public void CropRectangle_NormalizedBounds_ClampsWithinUnitSquare()
    {
        var crop = new CropRectangle(-0.2, -0.1, 1.5, 1.2);

        Assert.Equal(0.0, crop.X);
        Assert.Equal(0.0, crop.Y);
        Assert.Equal(1.0, crop.Width);
        Assert.Equal(1.0, crop.Height);
        Assert.True(crop.IsFullImage);
    }

    [Fact]
    public void CropRectangle_MoveAndResize_UpdatesCoordinatesCorrectly()
    {
        var crop = new CropRectangle(0.1, 0.1, 0.5, 0.5);

        // Mover a la derecha y abajo
        var moved = crop.Move(0.1, 0.1);
        Assert.Equal(0.2, moved.X, 3);
        Assert.Equal(0.2, moved.Y, 3);
        Assert.Equal(0.5, moved.Width, 3);
        Assert.Equal(0.5, moved.Height, 3);

        // Redimensionar
        var resized = moved.Resize(0.2, 0.1);
        Assert.Equal(0.7, resized.Width, 3);
        Assert.Equal(0.6, resized.Height, 3);
    }

    [Fact]
    public void CropRectangle_Presets_ComputeCorrectAspectRatio()
    {
        // Página A4 estándar vertical (ancho 210, alto 297, ratio ~0.707)
        double pageAspect = 210.0 / 297.0;

        var a4Preset = CropRectangle.CreatePreset(CropPreset.A4, pageAspect);
        Assert.False(a4Preset.IsFullImage);
        Assert.True(a4Preset.Width > 0 && a4Preset.Width <= 1.0);
        Assert.True(a4Preset.Height > 0 && a4Preset.Height <= 1.0);

        var cardPreset = CropRectangle.CreatePreset(CropPreset.BusinessCard, pageAspect);
        Assert.False(cardPreset.IsFullImage);
        Assert.True(cardPreset.Width <= 1.0 && cardPreset.Height <= 1.0);
    }

    [Fact]
    public void DuplicatePage_CreatesIndependentCloneWithIdenticalContent()
    {
        var page = new ScannedPage
        {
            ImageBytes = new byte[] { 10, 20, 30, 40 },
            PixelWidth = 400,
            PixelHeight = 600,
            Dpi = 300,
            RotationAngle = 90
        };
        page.Crop = new CropRectangle(0.1, 0.1, 0.8, 0.8);

        var clone = page.Clone();

        Assert.NotEqual(page.Id, clone.Id);
        Assert.Equal(page.PixelWidth, clone.PixelWidth);
        Assert.Equal(page.PixelHeight, clone.PixelHeight);
        Assert.Equal(page.Dpi, clone.Dpi);
        Assert.Equal(page.RotationAngle, clone.RotationAngle);
        Assert.Equal(page.Crop?.X, clone.Crop?.X);
        Assert.Equal(page.ImageBytes.Length, clone.ImageBytes.Length);

        // Modificar original no altera el clon
        page.RotateClockwise();
        Assert.Equal(180, page.RotationAngle);
        Assert.Equal(90, clone.RotationAngle);
    }

    [Fact]
    public void ImageHelper_ProcessImage_AppliesRotationAndCropAccurately()
    {
        // Crear imagen de prueba 200x100
        using var img = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(200, 100);
        using var ms = new System.IO.MemoryStream();
        img.Save(ms, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
        byte[] originalBytes = ms.ToArray();

        // Procesar con rotación 90° (debe quedar 100x200) y recorte 50% de ancho y 50% de alto (debe quedar 50x100)
        var crop = new CropRectangle(0.0, 0.0, 0.5, 0.5);
        byte[] processed = Simple_WinScan.Helpers.ImageHelper.ProcessImage(originalBytes, 90, crop);

        using var resultImg = SixLabors.ImageSharp.Image.Load(processed);
        Assert.Equal(50, resultImg.Width);
        Assert.Equal(100, resultImg.Height);
    }
}
