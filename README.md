# WinSimpleScan 📄🔍

> Clon moderno, nativo y optimizado de **GNOME Document Scanner (Simple Scan)** para **Windows 11 y 10**, desarrollado en **WinUI 3 (Windows App SDK)** y **C# (.NET 8)**.

[![Platform](https://img.shields.io/badge/Platform-Windows%2011%20%7C%2010%20x64-0078D4?logo=windows)](https://microsoft.com)
[![Framework](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![UI](https://img.shields.io/badge/UI-WinUI%203%20(Windows%20App%20SDK)-0078D7)](https://learn.microsoft.com/windows/apps/winui/winui3/)
[![Version](https://img.shields.io/badge/Version-2.0.0-blue.svg)](https://github.com/ARELL31/Simple-WinScan/releases)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

---

## ✨ Características Principales (Versión 2.0.0)

### 🖨️ Captura y Control de Hardware (WIA 2.0)
- **Experiencia de "Cero Fricción" (One-Click Scan):** Abre la aplicación, pulsa Escanear y tu documento digitalizado aparece al instante sin configuraciones previas engorrosas.
- **Detección y Refresco en Caliente (#1):** Detección automática al conectar un escáner USB y botón de refresco explícito (<kbd>F5</kbd>). Recuerda automáticamente el último dispositivo utilizado.
- **Reporte Fiel de Hardware y Modo Simulación (#2):**
  - Eliminación absoluta de falsos éxitos: los fallos mecánicos (lámpara, tapa abierta, desconexión) informan su causa real mediante un InfoBar.
  - Simulador explícito aislado con marca de agua distintiva para desarrollo y pruebas sin requerir escáner físico.
- **Cancelación Inmediata y Progreso Real (#3):** Cancelación asíncrona sin congelar la ventana. La barra de progreso solo reporta porcentaje cuando el controlador WIA emite bytes reales.
- **Modo Libro Continuo en Cama Plana (#4):** Escaneo con temporizador de cuenta regresiva (segundos configurables) y aviso sonoro para digitalizar libros con ambas manos libres.
- **Lotes por Alimentador ADF con Tolerancia a Fallos (#5):** Digitalización continua desde ADF hasta bandeja vacía (`WIA_ERROR_PAPER_EMPTY`). Si ocurre un atasco, todas las páginas ya capturadas se conservan intactas en el lienzo permitiendo continuar o guardar.
- **Diálogo de Ajustes y Capacidades (#6):** Control fino de DPI (75, 150, 300, 600, 1200), tamaño de papel (A4, Carta, Legal, Auto), brillo y contraste (-100 a +100), acotados según las capacidades reales del hardware.

### ✂️ Edición y Gestión de Documentos
- **Recorte Interactivo No Destructivo (#7):** Editor visual con asas para ratón, presets de relación de aspecto (A4, Carta, 4x6", Tarjeta, Libre) y ajuste fino con teclas de dirección para accesibilidad.
- **Portapapeles vs Duplicación (#7 / Auditoría):**
  - **Copiar al Portapapeles (<kbd>Ctrl+C</kbd>):** Coloca el mapa de bits recortado en el portapapeles del sistema para pegar en Word, Paint, chat, etc.
  - **Duplicar Página (<kbd>Ctrl+D</kbd>):** Crea una réplica de la página dentro del documento actual.
- **Reordenamiento Avanzado de Páginas (#8):**
  - Mover páginas individualmente a izquierda o derecha.
  - **Invertir Documento:** Invierte el orden completo de todas las páginas (1..N → N..1).
  - **Intercalar Dúplex:** Ideal para alimentadores simples; intercala páginas frontales (1, 3, 5...) con reversos (2, 4, 6...).
  - **Rotar Pares/Impares:** Rota 180° en bloque las páginas pares o impares seleccionadas.

### 💾 Exportación, Impresión e Integración con Windows
- **Exportación Multipropósito (#9):**
  - PDF multipágina de alta definición mediante **PdfSharpCore**.
  - Imágenes individuales en **JPEG**, **PNG** y **WebP** con calidad ajustable y numeración automática de lotes (`Doc_1.jpg`, `Doc_2.jpg`).
  - Paridad visual absoluta: la rotación y el recorte se aplican de forma idéntica en vista previa, PDF, imagen e impresión.
- **Impresión Nativa Directa (#10):** Impresión directa con diálogo nativo de impresoras de Windows Spooler (<kbd>Ctrl+P</kbd>).
- **Integración con Windows 11 (#11):**
  - **Visor Externo:** Doble clic para abrir la página en la aplicación "Fotos" predeterminada.
  - **Compartir Nativo:** Panel Compartir de Windows 11 (<kbd>Ctrl+H</kbd>) para enviar vía WhatsApp, Teams, Mail o Nearby Share.
  - **Correo Electrónico:** Envío directo mediante cliente predeterminado (`mailto:` con adjunto temporal).

### 🛡️ Resiliencia, Posprocesamiento y OCR
- **Autoguardado y Recuperación ante Cuelgues (#12):**
  - Control de documento modificado (`IsDirty`) que advierte antes de salir o limpiar.
  - Autoguardado periódico en segundo plano en `%LOCALAPPDATA%\WinSimpleScan\Autosaves`.
  - Detección de cierres inesperados al reiniciar, con opción de restaurar o descartar la sesión previa.
- **Gancho de Posprocesamiento Externo (#13):** Ejecución de scripts o herramientas externas (`.bat`, `.ps1`, `.exe`) con tokens `{file}` y `{dpi}`, conservando siempre la imagen original sin procesar como respaldo seguro ante errores.
- **OCR Nativo para PDF con Texto Buscable (#14):** Reconocimiento óptico de caracteres mediante la API nativa `Windows.Media.Ocr.OcrEngine` de Windows 10/11. Genera una capa de texto invisible exactamente alineada sobre la imagen para seleccionar y buscar palabras (<kbd>Ctrl+F</kbd>) en Edge, Chrome o Acrobat.

---

## ⌨️ Atajos de Teclado

| Atajo | Acción |
| :--- | :--- |
| <kbd>Ctrl</kbd> + <kbd>1</kbd> | Escanear página principal (Cama plana / Dispositivo activo) |
| <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>1</kbd> | Escanear lote continuo desde Alimentador (ADF) |
| <kbd>Escape</kbd> | Cancelar digitalización en curso |
| <kbd>F5</kbd> | Refrescar lista de escáneres |
| <kbd>Ctrl</kbd> + <kbd>S</kbd> | Guardar documento (PDF / JPEG / PNG / WebP) |
| <kbd>Ctrl</kbd> + <kbd>P</kbd> | Imprimir documento |
| <kbd>Ctrl</kbd> + <kbd>K</kbd> | Activar/Desactivar herramienta de recorte en la página activa |
| <kbd>Ctrl</kbd> + <kbd>C</kbd> | Copiar mapa de bits de la página activa al Portapapeles |
| <kbd>Ctrl</kbd> + <kbd>D</kbd> | Duplicar página activa dentro del documento |
| <kbd>Ctrl</kbd> + <kbd>[</kbd> | Rotar página activa 90° a la izquierda (antihorario) |
| <kbd>Ctrl</kbd> + <kbd>]</kbd> | Rotar página activa 90° a la derecha (horario) |
| <kbd>Ctrl</kbd> + <kbd>←</kbd> | Mover página activa a la izquierda |
| <kbd>Ctrl</kbd> + <kbd>→</kbd> | Mover página activa a la derecha |
| <kbd>Ctrl</kbd> + <kbd>R</kbd> | Abrir diálogo de Reordenamiento Avanzado (Invertir, Dúplex, Flip) |
| <kbd>Ctrl</kbd> + <kbd>H</kbd> | Abrir menú nativo Compartir de Windows 11 |
| <kbd>Ctrl</kbd> + <kbd>N</kbd> | Nuevo documento (solicita confirmación si hay cambios pendientes) |
| <kbd>Supr</kbd> / <kbd>Delete</kbd> | Eliminar página activa |

---

## 🏗️ Arquitectura de Software

La aplicación sigue el patrón **MVVM (Model-View-ViewModel)** estructurado en capas desacopladas:

```
Simple-WinScan/
├── Controls/               # Controles XAML reutilizables (PageCardControl, CropOverlayControl)
├── Helpers/                # Utilidades de ventana, reordenamiento, exportación e imágenes
├── Models/                 # Entidades del dominio (ScanProfile, ScannerDeviceInfo, ScannedPage, CropRectangle)
├── Services/
│   ├── Interfaces/         # Contratos desacoplados (IScannerService, IPdfExportService, IImageExportService,
│   │                       # IPrintService, IAutosaveService, ISystemIntegrationService, IPostprocessingService,
│   │                       # IOcrService, ISettingsService, IDialogService)
│   └── Implementations/    # WiaScannerService (COM STA nativo), MockScannerService (simulador explícito),
│                           # PdfSharpExportService, ImageSharpExportService, WindowsPrintService,
│                           # JsonAutosaveService, WindowsOcrService, PostprocessingService, SettingsService
├── ViewModels/             # MainViewModel, ScannedPageItemViewModel, ScanSettingsViewModel (CommunityToolkit.Mvvm)
├── Views/                  # MainWindow, ScanSettingsDialog, ReorderPagesDialog, PrintDocumentDialog
├── App.xaml / App.xaml.cs  # Inyección de dependencias (Microsoft.Extensions.DependencyInjection)
└── WinSimpleScan.csproj    # Compilación unpackaged WinUI 3 (WindowsPackageType=None)
```

---

## 🚀 Requisitos e Instalación

### Requisitos del Sistema:
- **Sistema Operativo:** Windows 10 (versión 1809 o posterior) o **Windows 11** (recomendado, versión de 64 bits).
- **Arquitectura:** x64.
- **Controlador de Escáner:** Escáner compatible con controlador WIA 2.0 (la gran mayoría de escáneres USB y de red en Windows), o utilizar el simulador virtual integrado.
- **Reconocimiento de Texto (OCR):** Requiere tener instalado el paquete de idioma correspondiente en Windows (`Configuración > Hora e idioma > Idioma`).

### Opciones de Instalación:
1. **Instalador Ejecutable (Recomendado):**  
   Descarga `WinSimpleScan-2.0.0-Setup-x64.exe` desde la sección de [Releases](https://github.com/ARELL31/Simple-WinScan/releases) y ejecuta el asistente de instalación.
2. **Paquete Portable (Sin instalación):**  
   Descarga `WinSimpleScan-2.0.0-win-x64-portable.zip`, descomprime en cualquier carpeta y ejecuta directamente `Simple-WinScan.exe`.

---

## 🛠️ Compilación desde el Código Fuente

### Prerrequisitos de Desarrollo:
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (versión 8.0.400 o superior).
- Visual Studio 2022 o Visual Studio Code con soporte de C# y Windows App SDK.
- Inno Setup 6 (opcional, para compilar el instalador).

### Compilar y Ejecutar en Modo Desarrollo:
```powershell
# Clonar el repositorio
git clone https://github.com/ARELL31/Simple-WinScan.git
cd Simple-WinScan

# Ejecutar el proyecto
dotnet run --project Simple-WinScan/Simple-WinScan.csproj -p:Platform=x64
```

### Ejecutar Pruebas Automatizadas:
```powershell
dotnet test Simple-WinScan.Tests/Simple-WinScan.Tests.csproj -p:Platform=x64
```

### Publicar Paquete de Producción Release:
```powershell
dotnet publish Simple-WinScan/Simple-WinScan.csproj -c Release -r win-x64 --self-contained true
```

---

## 👨‍💻 Autor

Desarrollado y mantenido por **[ARELL31](https://github.com/ARELL31)**.

## 📄 Licencia

Este proyecto está bajo la Licencia MIT. Consulta el archivo [LICENSE](LICENSE) para más detalles.
