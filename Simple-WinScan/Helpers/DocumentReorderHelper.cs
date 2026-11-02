using System;
using System.Collections.Generic;
using System.Linq;
using Simple_WinScan.ViewModels;

namespace Simple_WinScan.Helpers;

public static class DocumentReorderHelper
{
    public static List<ScannedPageItemViewModel> Reverse(IReadOnlyList<ScannedPageItemViewModel> pages)
    {
        if (pages == null || pages.Count <= 1) return pages?.ToList() ?? new List<ScannedPageItemViewModel>();

        var reversed = pages.Reverse().ToList();
        Renumber(reversed);
        return reversed;
    }

    public static List<ScannedPageItemViewModel> Collate(IReadOnlyList<ScannedPageItemViewModel> pages, bool reverseSecondHalf)
    {
        if (pages == null || pages.Count <= 1) return pages?.ToList() ?? new List<ScannedPageItemViewModel>();

        int half = (pages.Count + 1) / 2;
        var firstHalf = pages.Take(half).ToList();
        var secondHalf = pages.Skip(half).ToList();

        if (reverseSecondHalf)
        {
            secondHalf.Reverse();
        }

        var result = new List<ScannedPageItemViewModel>(pages.Count);
        int maxLen = Math.Max(firstHalf.Count, secondHalf.Count);

        for (int i = 0; i < maxLen; i++)
        {
            if (i < firstHalf.Count)
            {
                result.Add(firstHalf[i]);
            }
            if (i < secondHalf.Count)
            {
                result.Add(secondHalf[i]);
            }
        }

        Renumber(result);
        return result;
    }

    public static void RotateAlternates(IReadOnlyList<ScannedPageItemViewModel> pages, bool evenPages, int angleDelta = 180)
    {
        if (pages == null) return;

        for (int i = 0; i < pages.Count; i++)
        {
            int pageNumber = i + 1;
            bool isEven = (pageNumber % 2 == 0);

            if ((evenPages && isEven) || (!evenPages && !isEven))
            {
                int steps = ((angleDelta % 360 + 360) % 360) / 90;
                for (int s = 0; s < steps; s++)
                {
                    pages[i].RotateClockwise();
                }
            }
        }
    }

    private static void Renumber(List<ScannedPageItemViewModel> pages)
    {
        for (int i = 0; i < pages.Count; i++)
        {
            pages[i].PageNumber = i + 1;
        }
    }
}
