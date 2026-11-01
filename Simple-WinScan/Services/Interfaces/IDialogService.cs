using System.Threading.Tasks;

namespace Simple_WinScan.Services.Interfaces;

public interface IDialogService
{
    Task<string?> PickSavePdfPathAsync(string defaultFileName);
    Task ShowMessageAsync(string title, string message);
    Task<bool> ShowConfirmationAsync(string title, string message, string confirmText, string cancelText);
}
