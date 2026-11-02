using System.Collections.Generic;
using System.Threading.Tasks;
using Simple_WinScan.Models;

namespace Simple_WinScan.Services.Interfaces;

public interface ISystemIntegrationService
{
    Task<bool> OpenInExternalViewerAsync(ScannedPage page);
    Task<bool> OpenDocumentInExternalViewerAsync(IReadOnlyList<ScannedPage> pages);
    Task<bool> EmailPagesAsync(IReadOnlyList<ScannedPage> pages, string subject = "Documento Escaneado");
    Task<bool> SharePagesAsync(IReadOnlyList<ScannedPage> pages, string title = "Documento Escaneado");
}
