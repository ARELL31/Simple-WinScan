using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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

    public (byte[] RawBytes, int Width, int Height) TransferImage(ScannerDeviceInfo scanner, ScanProfile profile, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

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

        dynamic item = targetDevice.Items[1];
        dynamic itemProps = item.Properties;

        // Configurar intención de color (1=Color, 2=Grayscale, 4=B&W)
        SetWiaProperty(itemProps, 6146, (int)profile.ColorMode); // WIA_IPS_CUR_INTENT
        // Configurar resolución DPI
        SetWiaProperty(itemProps, 6147, profile.Dpi); // WIA_IPS_XRES
        SetWiaProperty(itemProps, 6148, profile.Dpi); // WIA_IPS_YRES

        cancellationToken.ThrowIfCancellationRequested();

        // Transferir imagen mediante WIA
        var dialogType = Type.GetTypeFromProgID("WIA.CommonDialog");
        dynamic? commonDialog = dialogType != null ? Activator.CreateInstance(dialogType) : null;
        dynamic imageFile;

        if (commonDialog != null)
        {
            imageFile = commonDialog.ShowTransfer(item, WiaPngFormatGuid, false);
        }
        else
        {
            imageFile = item.Transfer(WiaPngFormatGuid);
        }

        byte[] rawBytes = imageFile.FileData?.BinaryData is byte[] data ? data : Array.Empty<byte>();
        int width = (int)imageFile.Width;
        int height = (int)imageFile.Height;

        return (rawBytes, width, height);
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
}
