using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using Simple_WinScan.Models;
using Simple_WinScan.Services.Exceptions;

namespace Simple_WinScan.Services.Implementations;

public class DefaultWiaNativeInterop : IWiaNativeInterop
{
    private const string WiaPngFormatGuid = "{B96B3CAF-0728-11D3-9D7B-0000F81EF32E}";
    private const int ScannerDeviceType = 1;

    public IReadOnlyList<ScannerDeviceInfo> EnumerateDevices()
    {
        var result = new List<ScannerDeviceInfo>();
        try
        {
            var devMgrType = Type.GetTypeFromProgID("WIA.DeviceManager");
            if (devMgrType == null)
            {
                return result;
            }

            dynamic? devMgr = Activator.CreateInstance(devMgrType);
            if (devMgr == null)
            {
                return result;
            }

            dynamic deviceInfos = devMgr.DeviceInfos;
            int count = (int)deviceInfos.Count;
            for (int i = 1; i <= count; i++)
            {
                dynamic info = deviceInfos[i];
                int type = (int)info.Type;
                if (type == ScannerDeviceType)
                {
                    string deviceId = (string)info.DeviceID;
                    dynamic properties = info.Properties;
                    string name = GetWiaPropertyValue(properties, "Name") ?? "Escáner WIA";
                    string description = GetWiaPropertyValue(properties, "Description") ?? string.Empty;
                    string manufacturer = GetWiaPropertyValue(properties, "Manufacturer") ?? string.Empty;

                    result.Add(new ScannerDeviceInfo
                    {
                        Id = deviceId,
                        Name = name,
                        Description = description,
                        Manufacturer = manufacturer,
                        IsDefault = result.Count == 0
                    });
                }
            }
        }
        catch (COMException ex)
        {
            throw ScannerHardwareException.FromComException(ex);
        }

        return result;
    }

    public (byte[] RawBytes, int Width, int Height) TransferImage(
        ScannerDeviceInfo scanner,
        ScanProfile profile,
        IProgress<int>? progress,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(0);

        var devMgrType = Type.GetTypeFromProgID("WIA.DeviceManager");
        if (devMgrType == null)
        {
            throw new ScannerHardwareException("WIA.DeviceManager no está disponible en este sistema Windows.", unchecked((int)0x80040154), scanner.Id, scanner.Name);
        }

        dynamic? devMgr = Activator.CreateInstance(devMgrType);
        if (devMgr == null)
        {
            throw new ScannerHardwareException("No se pudo instanciar el administrador de dispositivos WIA.", unchecked((int)0x80004005), scanner.Id, scanner.Name);
        }

        dynamic? targetDevice = null;
        dynamic deviceInfos = devMgr.DeviceInfos;
        int count = (int)deviceInfos.Count;
        for (int i = 1; i <= count; i++)
        {
            dynamic info = deviceInfos[i];
            if ((string)info.DeviceID == scanner.Id)
            {
                targetDevice = info.Connect();
                break;
            }
        }

        if (targetDevice == null)
        {
            throw new ScannerHardwareException($"No se pudo conectar al escáner '{scanner.Name}'. Verifique que esté encendido y conectado.", unchecked((int)0x80210005), scanner.Id, scanner.Name);
        }

        try
        {
            dynamic devProps = targetDevice.Properties;
            int handling = profile.Source switch
            {
                ScanSource.Feeder => 1,
                ScanSource.Duplex => 4,
                _ => 2
            };
            SetWiaProperty(devProps, 3088, handling); // WIA_DPS_DOCUMENT_HANDLING_SELECT
        }
        catch { }

        dynamic item = targetDevice.Items[1];
        dynamic itemProps = item.Properties;

        // Configurar intención de color (1=Color, 2=Grayscale, 4=B&W)
        SetWiaProperty(itemProps, 6146, (int)profile.ColorMode); // WIA_IPS_CUR_INTENT
        // Configurar resolución DPI
        SetWiaProperty(itemProps, 6147, profile.Dpi); // WIA_IPS_XRES
        SetWiaProperty(itemProps, 6148, profile.Dpi); // WIA_IPS_YRES

        cancellationToken.ThrowIfCancellationRequested();

        // 1. Intentar transferencia WIA 2.0 basada en flujo asíncrono con IWiaTransfer
        if (item is IWiaTransfer wiaTransfer)
        {
            using var reg = cancellationToken.Register(() =>
            {
                try { wiaTransfer.Cancel(); } catch { }
            });

            var callback = new ManagedWiaTransferCallback(progress, cancellationToken, wiaTransfer);
            int hr = wiaTransfer.Download(0, callback);

            if (cancellationToken.IsCancellationRequested || hr == unchecked((int)0x800704C7) || hr == unchecked((int)0x80004004))
            {
                throw new OperationCanceledException("La captura fue cancelada por el usuario.", cancellationToken);
            }

            if (hr != 0)
            {
                throw new COMException("Fallo en la descarga de imagen WIA.", hr);
            }

            if (callback.Stream != null)
            {
                byte[] bytes = ReadStreamToBytes(callback.Stream);

                int w = (int)(8.27 * profile.Dpi);
                int h = (int)(11.69 * profile.Dpi);
                return (bytes, w, h);
            }
        }

        // 2. Ruta alternativa mediante WIA Automation (progreso indeterminado al no haber callback cuantitativo)
        var dialogType = Type.GetTypeFromProgID("WIA.CommonDialog");
        dynamic? commonDialog = dialogType != null ? Activator.CreateInstance(dialogType) : null;
        dynamic imageFile;

        cancellationToken.ThrowIfCancellationRequested();

        if (commonDialog != null)
        {
            // CancelError = true para que el diálogo WIA lance COMException ante cancelación
            imageFile = commonDialog.ShowTransfer(item, WiaPngFormatGuid, true);
        }
        else
        {
            imageFile = item.Transfer(WiaPngFormatGuid);
        }

        cancellationToken.ThrowIfCancellationRequested();

        byte[] rawBytes = imageFile.FileData?.BinaryData is byte[] data ? data : Array.Empty<byte>();
        int width = (int)imageFile.Width;
        int height = (int)imageFile.Height;

        return (rawBytes, width, height);
    }

    private static byte[] ReadStreamToBytes(IStream stream)
    {
        stream.Stat(out var statstg, 1);
        int size = (int)statstg.cbSize;
        byte[] buffer = new byte[size];

        stream.Seek(0, 0, IntPtr.Zero);
        IntPtr pcbRead = Marshal.AllocHGlobal(sizeof(int));
        try
        {
            stream.Read(buffer, size, pcbRead);
        }
        finally
        {
            Marshal.FreeHGlobal(pcbRead);
        }

        return buffer;
    }

    private static void SetWiaProperty(dynamic properties, int propId, object value)
    {
        try
        {
            int count = (int)properties.Count;
            for (int i = 1; i <= count; i++)
            {
                dynamic prop = properties[i];
                if ((int)prop.PropertyID == propId)
                {
                    prop.Value = value;
                    break;
                }
            }
        }
        catch
        {
            // Propiedad opcional no soportada por el driver del escáner
        }
    }

    private static string? GetWiaPropertyValue(dynamic properties, string propName)
    {
        try
        {
            int count = (int)properties.Count;
            for (int i = 1; i <= count; i++)
            {
                dynamic prop = properties[i];
                if (string.Equals((string)prop.Name, propName, StringComparison.OrdinalIgnoreCase))
                {
                    return prop.Value?.ToString();
                }
            }
        }
        catch
        {
            // Propiedad opcional
        }
        return null;
    }

    #region Definiciones COM WIA 2.0 para IWiaTransfer e IWiaTransferCallback

    [DllImport("ole32.dll", PreserveSig = false)]
    private static extern void CreateStreamOnHGlobal(IntPtr hGlobal, bool fDeleteOnRelease, out IStream ppstm);

    [ComImport]
    [Guid("c39d6942-2f4e-4d04-92fe-4ef4d3a1de5a")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IWiaTransfer
    {
        [PreserveSig]
        int Download(int lFlags, [MarshalAs(UnmanagedType.Interface)] IWiaTransferCallback pIWiaTransferCallback);

        [PreserveSig]
        int Upload(int lFlags, [MarshalAs(UnmanagedType.Interface)] IStream pSource, [MarshalAs(UnmanagedType.Interface)] IWiaTransferCallback pIWiaTransferCallback);

        [PreserveSig]
        int Cancel();

        [PreserveSig]
        int EnumWIA_FORMAT_INFO(out IntPtr ppEnum);
    }

    [ComImport]
    [Guid("27d4eaaf-28a6-4ca5-9aab-e678168b9527")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IWiaTransferCallback
    {
        [PreserveSig]
        int TransferCallback(int lFlags, ref WiaTransferParams pWiaTransferParams);

        [PreserveSig]
        int GetNextStream(int lFlags, [MarshalAs(UnmanagedType.BStr)] string bstrItemName, [MarshalAs(UnmanagedType.BStr)] string bstrFullItemName, out IStream ppDestination);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WiaTransferParams
    {
        public int lMessage;
        public int lPercentComplete;
        public ulong ulTransferredBytes;
        public int hrErrorStatus;
    }

    private class ManagedWiaTransferCallback : IWiaTransferCallback
    {
        private readonly IProgress<int>? _progress;
        private readonly CancellationToken _cancellationToken;
        private readonly IWiaTransfer? _wiaTransfer;
        public IStream? Stream { get; private set; }

        public ManagedWiaTransferCallback(IProgress<int>? progress, CancellationToken cancellationToken, IWiaTransfer? wiaTransfer)
        {
            _progress = progress;
            _cancellationToken = cancellationToken;
            _wiaTransfer = wiaTransfer;
        }

        public int TransferCallback(int lFlags, ref WiaTransferParams pWiaTransferParams)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _wiaTransfer?.Cancel();
                return unchecked((int)0x800704C7); // ERROR_CANCELLED
            }

            if (pWiaTransferParams.lPercentComplete >= 0 && pWiaTransferParams.lPercentComplete <= 100)
            {
                _progress?.Report(pWiaTransferParams.lPercentComplete);
            }

            return 0; // S_OK
        }

        public int GetNextStream(int lFlags, string bstrItemName, string bstrFullItemName, out IStream ppDestination)
        {
            CreateStreamOnHGlobal(IntPtr.Zero, true, out var stream);
            Stream = stream;
            ppDestination = stream;
            return 0; // S_OK
        }
    }

    #endregion
}
