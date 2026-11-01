# WinSimpleScan - Documento de Arquitectura y Plan de Implementación

> **Proyecto:** WinSimpleScan (Clon de GNOME Simple Scan para Windows 11)  
> **Harness & Metodología:** GitHub Superpowers + UI/UX Pro Max  
> **Stack:** C# (.NET 8.0), WinUI 3 (Windows App SDK 2.6.4), CommunityToolkit.Mvvm, WIA 2.0 COM, PdfSharpCore  
> **Fecha:** 2026-11-01  

---

## 1. FASE 1: DISEÑO DE INTERFAZ Y EXPERIENCIA (UI/UX PRO MAX)

### 1.1 Filosofía de UX: "Cero Fricción" (Estilo GNOME Simple Scan)
El principal valor de GNOME Simple Scan es la inmediatez. No existen asistentes de configuración ni pantallas intermedias:
1. **Lanzamiento instantáneo:** El usuario abre la aplicación y el escáner predeterminado ya está seleccionado e inicializado en segundo plano.
2. **Acción directa:** Un botón principal de acento `"Escanear"` visible sin navegación. Al pulsarlo:
   - Se inicia la captura con los ajustes predeterminados (Modo Texto: 300 DPI, escala de grises / contraste optimizado).
   - El lienzo central muestra un esqueleto de carga y la primera página aparece en cuanto se transfieren los primeros bytes.
3. **Flujo de digitalización continuo:** Cada escaneo adicional agrega una nueva página a la pila de documentos.
4. **Guardado en un clic:** Botón prominente `"Guardar PDF"`. Al hacer clic, abre el selector nativo sugiriendo un nombre estándar (`Documento_2026-11-01.pdf`).
5. **Edición contextual sin salir del flujo:** Rotar (90° izq/der), reordenar y eliminar páginas directamente sobre las tarjetas del lienzo o desde la barra superior.

### 1.2 Jerarquía Visual y Fluent Design (Windows 11)
* **Material Mica (`MicaBackdrop`):** Fondo reactivo a la luz y al papel tapiz de Windows 11.
* **Barra de título integrada (`ExtendsContentIntoTitleBar = true`):**
  - Área izquierda: Icono de escáner y título de la app `WinSimpleScan`.
  - Área derecha: Espacio reservado para los botones nativos de Minimizar/Maximizar/Cerrar de Windows.
* **CommandBar Superior (Limpia y funcional):**
  - *Leading (Izquierda):*
    - Botón primario de acento: `[Escanear]` (Icono `&#xEC59;` / `DocumentScanner`).
    - Selector rápido de perfil: Segmented / DropDown entre `"Texto"` (300 DPI, PDF ligero) y `"Foto"` (600 DPI, Color).
    - Selector discreto de escáner (`ComboBox` con icono `&#xE749;`).
  - *Center (Centro):*
    - Contador de páginas con tipografía sutil: `"3 páginas escaneadas"`.
  - *Trailing (Derecha):*
    - Botón primario secundario: `[Guardar PDF]` (Icono `&#xE74E;`).
    - Herramientas de página (habilitadas cuando hay una página seleccionada):
      - Rotar Izquierda (`&#xE7A7;`).
      - Rotar Derecha (`&#xE7A6;`).
      - Eliminar Página (`&#xE74D;`).
    - Menú secundario `[...]` (Preferencias, información, limpiar documento).
* **Lienzo Central (`ScrollViewer` + `ItemsRepeater`):**
  - **Estado Vacío (0 páginas):**
    - Contenedor centrado con icono de escáner sutil (`&#xE749;`), título `"No hay páginas escaneadas"`, subtítulo explicativo y un botón grande centrado `"Comenzar a escanear"`.
  - **Estado con Documento (1+ páginas):**
    - Pila de hojas verticales con sombra de elevación (`ThemeShadow` / `AttachedCardShadow`), esquinas redondeadas (`CornerRadius="4"`), fondo blanco tipo papel (`#FFFFFF` en Light Mode / `#2D2D2D` en Dark Mode).
    - Barra flotante contextual al pasar el ratón (hover) sobre cada miniatura: botones rápidos de rotar y eliminar.
* **Feedback de Hardware:**
  - `ProgressBar` horizontal indeterminada en la parte superior del canvas mientras el escáner físico está activo.
  - `InfoBar` nativo no invasivo para advertencias (ej. "Tapa abierta", "No se detectó escáner", "Escaneando página 2...").

---

## 2. FASE 2: ARQUITECTURA DE SOFTWARE (C# / .NET 8 / WinUI 3)

### 2.1 Patrón MVVM (CommunityToolkit.Mvvm)
* **Inyección de Dependencias (`Microsoft.Extensions.DependencyInjection`):**
  - Registro de servicios singleton (`IScannerService`, `IPdfExportService`, `IDialogService`).
  - ViewModels registrados como transient o singleton según su ciclo de vida.
* **Separación de responsabilidades:**
  - `MainWindow.xaml.cs` únicamente resuelve el ViewModel y configura el TitleBar / HWND interop.
  - Toda la lógica reside en `MainViewModel` utilizando `[ObservableProperty]` y `[RelayCommand]`.
  - Data Binding compilado mediante `x:Bind` con `Mode=OneWay` para alto rendimiento y cero reflexión.

### 2.2 Capa de Hardware: Windows Image Acquisition (WIA) 2.0
* **Interfaz `IScannerService`:**
  ```csharp
  public interface IScannerService
  {
      Task<IReadOnlyList<ScannerDeviceInfo>> GetAvailableScannersAsync();
      Task<ScannedPageData> ScanPageAsync(string deviceId, ScanProfile profile, CancellationToken cancellationToken = default);
      bool IsAvailable { get; }
  }
  ```
* **Implementación `WiaScannerService`:**
  - Utiliza el modelo COM de `WIA 2.0` (`wiaaut.dll` / `WIA.DeviceManager`).
  - La comunicación COM con hardware se aísla en un hilo dedicado con modelo de apartamentos STA (`ApartmentState.STA`), garantizando estabilidad y evitando bloqueos del hilo de UI (`DispatcherQueue`).
  - **Modo Virtual / Mock integrado:** Si la máquina no tiene un escáner físico conectado durante el desarrollo o pruebas, el servicio incluye un emulador que genera páginas de prueba de alta calidad, permitiendo probar el 100% del software sin hardware conectado.

### 2.3 Capa de Procesamiento y Exportación PDF
* **Manejo en memoria:**
  - Cada página escaneada se almacena como `ScannedPage` con sus bytes originales sin compresión con pérdidas previa (PNG / raw BMP), ángulo de rotación (0°, 90°, 180°, 270°) y miniatura `SoftwareBitmapSource` / `WriteableBitmap` para renderizado instantáneo en WinUI 3.
* **Ensamblado PDF (`PdfSharpCore`):**
  - Biblioteca ligera, 100% administrada en .NET Standard, sin dependencias nativas de C++ ni GDI+.
  - Cada página escaneada se mapea a una página PDF (`PdfPage`) con tamaño exacto (A4 / Letter según DPI y dimensiones en puntos tipográficos).
  - Admite compresión de flujo JPEG interna para generar PDFs compactos con calidad óptima.

### 2.4 Estructura de Directorios Propuesta

```text
Simple-WinScan/
├── Simple-WinScan.slnx
└── Simple-WinScan/
    ├── Simple-WinScan.csproj
    ├── App.xaml / App.xaml.cs
    ├── MainWindow.xaml / MainWindow.xaml.cs
    ├── Models/
    │   ├── ScannedPage.cs              // Representación de una página escaneada
    │   ├── ScannerDeviceInfo.cs        // Datos del escáner detectado
    │   └── ScanProfile.cs              // Perfil (Texto vs Foto, DPI, Color)
    ├── ViewModels/
    │   ├── MainViewModel.cs            // ViewModel orquestador principal
    │   └── ScannedPageItemViewModel.cs // ViewModel individual para cada miniatura
    ├── Services/
    │   ├── Interfaces/
    │   │   ├── IScannerService.cs      // Contrato de hardware de escaneo
    │   │   ├── IPdfExportService.cs    // Contrato de exportación a PDF
    │   │   └── IDialogService.cs       // Diálogos nativos (SavePicker, ContentDialog)
    │   └── Implementations/
    │       ├── WiaScannerService.cs    // Implementación COM WIA 2.0 + Fallback Mock
    │       ├── PdfSharpExportService.cs// Ensamblador PDF multipágina
    │       └── WinUiDialogService.cs   // Manejo de diálogos y Win32 HWND
    ├── Helpers/
    │   ├── WindowHelper.cs             // Configuración de Mica y TitleBar
    │   └── ImageHelper.cs              // Rotación y conversión de imágenes
    ├── Controls/
    │   └── PageCardControl.xaml        // Tarjeta visual interactiva para la página
    └── Assets/
        └── ...
```

---

## 3. PLAN DE EJECUCIÓN (FASE 3 - GITHUB SUPERPOWERS)

1. **Paso 1: Paquetes y Configuración del Proyecto**
   - Incorporar dependencias: `CommunityToolkit.Mvvm`, `Microsoft.Extensions.DependencyInjection`, `PdfSharpCore`.
2. **Paso 2: Modelos y Contratos de Servicio**
   - Crear `ScanProfile`, `ScannerDeviceInfo`, `ScannedPage` y las interfaces de servicio.
3. **Paso 3: Implementación del Motor de Hardware (`WiaScannerService`)**
   - Lógica de comunicación WIA 2.0 COM STA + Emulador Mock de prueba.
4. **Paso 4: Implementación del Motor de Exportación PDF (`PdfSharpExportService`)**
   - Ensamblaje multipágina a PDF y guardado con `FileSavePicker`.
5. **Paso 5: ViewModel Principal y Colecciones (`MainViewModel`)**
   - Comandos de escaneo, rotación, eliminación y guardado.
6. **Paso 6: Vista WinUI 3 Fluent (`MainWindow.xaml`)**
   - Integración con Mica, CommandBar completa, estado vacío y canvas interactivo.
7. **Paso 7: Compilación, Verificación y Commit en Repositorio**
   - Compilación con `dotnet build`, verificación de funcionamiento y commit estructurado con Git.
