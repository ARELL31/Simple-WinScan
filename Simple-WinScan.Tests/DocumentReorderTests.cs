using System.Collections.Generic;
using System.Linq;
using Simple_WinScan.Helpers;
using Simple_WinScan.Models;
using Simple_WinScan.ViewModels;
using Xunit;

namespace Simple_WinScan.Tests;

public class DocumentReorderTests
{
    private static List<ScannedPageItemViewModel> CreatePages(int count)
    {
        var list = new List<ScannedPageItemViewModel>();
        for (int i = 1; i <= count; i++)
        {
            var page = new ScannedPage
            {
                ImageBytes = new byte[] { (byte)i },
                PixelWidth = 100,
                PixelHeight = 100
            };
            list.Add(new ScannedPageItemViewModel(page, i));
        }
        return list;
    }

    [Fact]
    public void ReverseDocument_InvertsPageOrderCompletely()
    {
        var pages = CreatePages(4); // 1, 2, 3, 4

        var reordered = DocumentReorderHelper.Reverse(pages);

        var originalIds = reordered.Select(p => (int)p.Model.ImageBytes[0]).ToList();
        Assert.Equal(new[] { 4, 3, 2, 1 }, originalIds);
        Assert.Equal(new[] { 1, 2, 3, 4 }, reordered.Select(p => p.PageNumber).ToList());
    }

    [Fact]
    public void CollateDuplex_InterleavesFrontAndBackStacks()
    {
        // 6 páginas: primeras 3 son frentes (1, 2, 3), últimas 3 son reversos (4, 5, 6)
        var pages = CreatePages(6);

        // Sin invertir segunda mitad: Frente1, Dorso1, Frente2, Dorso2, Frente3, Dorso3 -> 1, 4, 2, 5, 3, 6
        var reordered = DocumentReorderHelper.Collate(pages, reverseSecondHalf: false);

        var originalNumbers = reordered.Select(p => (int)p.Model.ImageBytes[0]).ToList();
        Assert.Equal(new[] { 1, 4, 2, 5, 3, 6 }, originalNumbers);
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6 }, reordered.Select(p => p.PageNumber).ToList());
    }

    [Fact]
    public void CollateDuplex_WhenSecondHalfInverted_InterleavesCorrectly()
    {
        // 6 páginas: frentes 1, 2, 3; reversos alimentados al revés 6, 5, 4 (en la lista 4=Dorso3, 5=Dorso2, 6=Dorso1)
        var pages = CreatePages(6);

        // Con invertir segunda mitad: 1, 6, 2, 5, 3, 4
        var reordered = DocumentReorderHelper.Collate(pages, reverseSecondHalf: true);

        var originalNumbers = reordered.Select(p => (int)p.Model.ImageBytes[0]).ToList();
        Assert.Equal(new[] { 1, 6, 2, 5, 3, 4 }, originalNumbers);
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6 }, reordered.Select(p => p.PageNumber).ToList());
    }

    [Fact]
    public void RotateAlternatePages_RotatesEvenPagesOnly()
    {
        var pages = CreatePages(4);

        DocumentReorderHelper.RotateAlternates(pages, evenPages: true, angleDelta: 180);

        Assert.Equal(0, pages[0].RotationAngle);   // Página 1 (impar) no cambia
        Assert.Equal(180, pages[1].RotationAngle); // Página 2 (par) rotada
        Assert.Equal(0, pages[2].RotationAngle);   // Página 3 (impar) no cambia
        Assert.Equal(180, pages[3].RotationAngle); // Página 4 (par) rotada
    }

    [Fact]
    public void RotateAlternatePages_RotatesOddPagesOnly()
    {
        var pages = CreatePages(4);

        DocumentReorderHelper.RotateAlternates(pages, evenPages: false, angleDelta: 180);

        Assert.Equal(180, pages[0].RotationAngle); // Página 1 (impar) rotada
        Assert.Equal(0, pages[1].RotationAngle);   // Página 2 (par) no cambia
        Assert.Equal(180, pages[2].RotationAngle); // Página 3 (impar) rotada
        Assert.Equal(0, pages[3].RotationAngle);   // Página 4 (par) no cambia
    }
}
