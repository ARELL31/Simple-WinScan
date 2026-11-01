using System;
using System.Runtime.InteropServices;

namespace Simple_WinScan.Services.Exceptions;

/// <summary>
/// Excepción lanzada cuando ocurre un fallo físico o de comunicación en el escáner WIA.
/// </summary>
public class ScannerHardwareException : Exception
{
    public int ErrorCode { get; }
    public string? ScannerId { get; }
    public string? ScannerName { get; }

    public ScannerHardwareException(string message, int errorCode = 0, string? scannerId = null, string? scannerName = null, Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        ScannerId = scannerId;
        ScannerName = scannerName;
        HResult = errorCode;
    }

    /// <summary>
    /// Crea una excepción de hardware mapeando los códigos HRESULT estándares de WIA 2.0 a explicaciones en español.
    /// </summary>
    public static ScannerHardwareException FromComException(COMException ex, string? scannerId = null, string? scannerName = null)
    {
        int hr = ex.ErrorCode;
        string detail = hr switch
        {
            unchecked((int)0x80210001) => "Error general del escáner (WIA_ERROR_GENERAL_ERROR).",
            unchecked((int)0x80210002) => "Atasco de papel en el mecanismo del escáner (WIA_ERROR_PAPER_JAM).",
            unchecked((int)0x80210003) => "La bandeja o alimentador de papel está vacía (WIA_ERROR_PAPER_EMPTY).",
            unchecked((int)0x80210004) => "Problema en el paso de papel del alimentador (WIA_ERROR_PAPER_PROBLEM).",
            unchecked((int)0x80210005) => "El escáner está fuera de línea o desconectado (WIA_ERROR_OFFLINE).",
            unchecked((int)0x80210006) => "El escáner está ocupado con otra tarea (WIA_ERROR_BUSY).",
            unchecked((int)0x80210007) => "El escáner se está calentando, espere unos momentos (WIA_ERROR_WARMING_UP).",
            unchecked((int)0x80210008) => "El escáner requiere intervención del usuario, verifique la tapa (WIA_ERROR_USER_INTERVENTION).",
            unchecked((int)0x80210009) => "El elemento de escaneo seleccionado ya no está disponible (WIA_ERROR_ITEM_DELETED).",
            unchecked((int)0x8021000A) => "Error de comunicación con el escáner, verifique el cable USB o la red (WIA_ERROR_DEVICE_COMMUNICATION).",
            unchecked((int)0x8021000B) => "Comando o parámetro no admitido por el dispositivo (WIA_ERROR_INVALID_COMMAND).",
            unchecked((int)0x8021000C) => "Configuración de hardware incorrecta en el escáner (WIA_ERROR_INCORRECT_HARDWARE_SETTING).",
            unchecked((int)0x8021000D) => "El cabezal o carro del escáner se encuentra bloqueado (WIA_ERROR_DEVICE_LOCKED).",
            unchecked((int)0x8021000E) => "Excepción interna dentro del controlador del escáner (WIA_ERROR_EXCEPTION_IN_DRIVER).",
            unchecked((int)0x8021000F) => "Respuesta no válida del controlador del fabricante (WIA_ERROR_INVALID_DRIVER_RESPONSE).",
            _ => $"Fallo del dispositivo de escaneo (HRESULT: 0x{hr:X8}): {ex.Message}"
        };

        string scannerPrefix = !string.IsNullOrWhiteSpace(scannerName) ? $"[{scannerName}] " : string.Empty;
        return new ScannerHardwareException($"{scannerPrefix}{detail}", hr, scannerId, scannerName, ex);
    }
}
