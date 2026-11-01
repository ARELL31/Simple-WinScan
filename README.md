# WinSimpleScan 📄🔍

> Clon moderno, nativo y optimizado de **GNOME Simple Scan** para **Windows 11**, desarrollado en **WinUI 3 (Windows App SDK)** y **C# (.NET 8)**.

[![Platform](https://img.shields.io/badge/Platform-Windows%2011%20%7C%2010%20x64-0078D4?logo=windows)](https://microsoft.com)
[![Framework](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![UI](https://img.shields.io/badge/UI-WinUI%203%20(Windows%20App%20SDK)-0078D7)](https://learn.microsoft.com/windows/apps/winui/winui3/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

---

## ✨ Características Principales

- **Experiencia de "Cero Fricción" (One-Click Scan):** Abre la aplicación, presiona un botón y la página digitalizada aparece de inmediato sin configuraciones complejas iniciales.
- **Fluent Design Nativo para Windows 11:**
  - Fondo translúcido con material **Mica**.
  - Tipografía Segoe Fluent y glyphs consistentes con el lenguaje visual de Windows 11.
  - Barra de comandos superior compacta (`CommandBar`) y lienzo de páginas con sombra y bordes redondeados.
- **Compatibilidad Universal con Hardware (WIA 2.0 COM):**
  - Comunicación directa con escáneres físicos a través de **Windows Image Acquisition (WIA) 2.0** en hilos STA dedicados.
  - Detección automática de dispositivos conectados y selector dinámico.
  - **Modo Escáner Virtual Simulado:** Si no hay escáner físico conectado, activa un generador de escaneo en memoria para realizar pruebas de software completas.
- **Gestión Intuitiva de Páginas Multipágina:**
  - Reordenamiento visual de páginas (Mover izquierda / derecha).
  - Rotación en vivo en incrementos de 90° (horario y antihorario).
  - Eliminación de páginas no deseadas.
- **Exportación PDF Multipágina Ligera:**
  - Generación de documentos PDF compactos y de alta fidelidad mediante **PdfSharpCore**.
  - Ajuste proporcional de dimensiones según resolución DPI original.
- **Distribución 100% Portable (Unpackaged & Self-Contained):**
  - No requiere instalación de paquetes MSIX ni permisos elevados de administrador.

---

## 🏗️ Arquitectura de Software

La aplicación sigue estrictamente el patrón **MVVM (Model-View-ViewModel)** estructurado en capas desacopladas:

```
Simple-WinScan/
├── Controls/               # Controles XAML reutilizables (PageCardControl)
├── Helpers/                # Utilidades de ventana (WindowHelper) y conversión de imágenes
├── Models/                 # Entidades del dominio (ScanProfile, ScannerDeviceInfo, ScannedPage)
├── Properties/             # Perfiles de publicación y arranque
├── Services/
│   ├── Interfaces/         # IScannerService, IPdfExportService, IDialogService
│   └── Implementations/    # WiaScannerService (COM STA), PdfSharpExportService, WinUiDialogService
├── ViewModels/             # MainViewModel, ScannedPageItemViewModel (CommunityToolkit.Mvvm)
├── Views/                  # MainWindow (WinUI 3 XAML + C# Code-Behind)
├── App.xaml / App.xaml.cs  # Inyección de dependencias (Microsoft.Extensions.DependencyInjection)
└── WinSimpleScan.csproj    # Definición de compilación y empaquetado unpackaged
```

---

## 🚀 Requisitos y Compilación

### Requisitos previos:
- Windows 10 (versión 1809 o superior) o **Windows 11** (recomendado).
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
- Windows App SDK Runtime (incluido en modo autónomo / *self-contained*).

### Compilar y Ejecutar en Desarrollo:
```powershell
# Clonar el repositorio
git clone https://github.com/ARELL31/Simple-WinScan.git
cd Simple-WinScan

# Ejecutar el proyecto
dotnet run --project Simple-WinScan/Simple-WinScan.csproj
```

### Generar Binarios Portables para Producción:
```powershell
dotnet build Simple-WinScan/Simple-WinScan.csproj -c Release -r win-x64
```
Los archivos ejecutables listos para usar se generarán en:
`Simple-WinScan/bin/Release/net8.0-windows10.0.19041.0/win-x64/`

---

## 👨‍💻 Autor

Desarrollado y mantenido por **[ARELL31](https://github.com/ARELL31)**.

## 📄 Licencia

Este proyecto está bajo la Licencia MIT. Consulta el archivo [LICENSE](LICENSE) para más detalles.
