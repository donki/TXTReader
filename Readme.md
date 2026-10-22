# TXT Reader

![.NET MAUI](https://img.shields.io/badge/.NET%20MAUI-10.0-blue)
![Android](https://img.shields.io/badge/Android-7.0%2B-green)
![License](https://img.shields.io/badge/License-MIT-yellow)
![Status](https://img.shields.io/badge/Status-Active-brightgreen)

📖 **Lector de archivos de texto moderno y profesional para Android**

Una aplicación Android desarrollada con .NET MAUI que permite leer archivos de texto con funcionalidades avanzadas, compatible con servicios de almacenamiento en la nube y archivos locales.

## Dónde conseguirla

- **Google Play:** https://play.google.com/store/apps/details?id=com.socratic.txtreader
- **Releases de GitHub** (APK / EXE / MSIX de cada versión): https://github.com/donki/TXTReader/releases

## 📸 Capturas de Pantalla

| Pantalla Principal | Lector de Texto | Búsqueda en Tiempo Real |
|:--:|:--:|:--:|
| Lista de archivos recientes | Texto con zoom y selección | Resaltado automático |

> **Nota**: Las capturas de pantalla muestran la interfaz real de la aplicación con el tema azul unificado y navegación nativa de Android.

## ✨ Características Principales

### 🔍 **Lectura Avanzada**
- 📁 **Detección automática de codificación** - UTF-8, UTF-16, UTF-32, Windows-1252, ISO-8859-1
- 🔍 **Búsqueda en tiempo real** - Resaltado amarillo profesional mientras escribes
- 📝 **Selección de texto real** - Copia y pega como en cualquier navegador web
- 🔍 **Zoom intuitivo** - Barra deslizante vertical de 8px a 32px
- 📝 **Vista de Markdown** - Los `.md` se abren renderizados (títulos, listas y tareas, tablas, código, citas, enlaces, imágenes locales) con el tema claro u oscuro; el botón de la barra alterna con el texto tal cual en cualquier fichero

### ☁️ **Compatibilidad Total con la Nube**
- 🌐 **OneDrive** - Funciona como Microsoft Edge, sin archivos temporales
- 📱 **Google Drive** - Acceso directo a archivos en la nube
- 📦 **Otros servicios de almacenamiento** - Soporte para archivos compartidos desde sus apps
- 🔗 **URIs de Content** - Lectura nativa usando ContentResolver de Android

### 🚀 **Funcionalidades Avanzadas**
- 🕒 **Historial** - Los últimos 5 archivos abiertos (si uno ya no existe, se quita de la lista al tocarlo)
- 📱 **Intents de Android** - Abre archivos .log desde otras aplicaciones
- 🐛 **Sistema de logging** - Diagnóstico integrado para desarrollo
- 🎨 **Interfaz moderna** - Colores azules unificados y navegación nativa

## 📱 Plataformas Soportadas

- **Android** (API 24+ / Android 7.0+)
- Optimizado para Android 10+ con soporte completo para Scoped Storage

## 📂 Formatos de Archivo Soportados

La aplicación puede abrir cualquier archivo de texto, incluyendo:

- `.txt` - Archivos de texto plano
- `.log` - Archivos de registro
- `.json` - Archivos JSON
- `.xml` - Archivos XML
- `.csv` - Archivos CSV
- `.md` - Archivos Markdown
- `.ini` - Archivos de configuración INI
- `.cfg` - Archivos de configuración
- `.conf` - Archivos de configuración
- Y muchos más formatos de texto

## 🚀 Instalación

### Requisitos Previos

- .NET 10.0 o superior
- Visual Studio 2022 17.8+ con cargas de trabajo de .NET MAUI
- Android SDK 34+ y dispositivo/emulador Android

### Compilación

```bash
# Clonar el repositorio
git clone https://github.com/tu-usuario/TXTReader.git
cd TXTReader

# Restaurar dependencias
dotnet restore

# Compilar para Android
dotnet build -f net10.0-android

# Instalar en dispositivo Android conectado
dotnet build -f net10.0-android -t:Install

# Ejecutar en dispositivo
dotnet build -f net10.0-android -t:Run
```

### APK Release

Para generar un APK firmado para distribución:

```bash
dotnet publish -f net10.0-android -c Release
```

## 🎯 Uso

### 📂 Abrir Archivos

1. **Desde la aplicación**: Botón "📁 Seleccionar Archivo" en la pantalla principal
2. **Desde servicios en la nube**: 
   - OneDrive: Toca "Abrir con" → TXT Reader
   - Google Drive: Comparte → TXT Reader  
   - Otros servicios de almacenamiento: Exportar o Abrir con → TXT Reader
3. **Desde otras aplicaciones**: Abre archivos .log, .txt desde cualquier app
4. **Archivos recientes**: Lista con los últimos 5 archivos abiertos

### 🔍 Funciones del Lector

- **Búsqueda en tiempo real**: Escribe en el campo superior, resaltado automático
- **Zoom con barra**: Desliza la barra vertical para ajustar de 8px a 32px
- **Selección de texto**: Mantén presionado para seleccionar y copiar
- **Vista de Markdown**: los `.md` y `.markdown` se abren renderizados. El botón de la barra
  superior alterna: `</>` enseña el texto tal cual y el ojo vuelve a la vista renderizada (también
  sirve para ver como Markdown cualquier otro fichero). Los enlaces se abren en el navegador o la
  app que toque; los de dentro del documento (índice, notas al pie) saltan dentro de él. Las
  imágenes de la carpeta del documento se ven; las de internet no se descargan (todo es local) y
  quedan como un enlace con su texto. La búsqueda y el zoom funcionan igual en las dos vistas.
- **Navegación nativa**: Botón "atrás" de Android para regresar
- **Información técnica**: Codificación detectada en el título

## 🧪 Pruebas

227 pruebas automatizadas (xUnit), sin dispositivo ni red. Además de la lógica (detección de
codificación sobre ficheros temporales, recientes, resaltado de la búsqueda, idiomas con las mismas
claves en `AppResources.resx` y `AppResources.es.resx`, comprobación de versión), recorren las
pantallas con su XAML real compilado para `net10.0` con Microsoft.Maui.Controls: la principal
(selector, recientes, apertura, errores), el lector (carga, búsqueda, zoom, colores), Acerca de
(idioma, correo y sus alternativas, volver), el menú con el botón de atrás (Mobile §7), la apertura
desde otras apps (`Services/IntentFileHandler.cs`), el visor HTML y la vista de Markdown
(`Services/MarkdownRenderer.cs`, `ReaderHtml.cs` y `ReaderLinks.cs`: cada elemento, imágenes
locales y remotas, política sin red, colores de los dos temas, enlaces y el botón que alterna). Lo que pediría el dispositivo
(selector, correo, navegador, portapapeles, diálogos, `content://`) pasa por
`Services/AppPlatform.cs` y en las pruebas lo responden dobles.

| Fecha | Pruebas | Cobertura de lo instrumentado | Cobertura sobre toda la app | Tiempo del banco |
|---|---|---|---|---|
| 2026-10-22 | 227 (todas pasan) | 98,1 % (1203 / 1226 líneas) | **94,1 %** (1203 / 1278 líneas) | ~1 s de pruebas, ~5 s con el arranque de `dotnet test` |
| 2026-10-02 | 166 (todas pasan) | 97,9 % (889 / 908 líneas) | **92,6 %** (889 / 960 líneas) | ~1 s de pruebas, ~5 s con el arranque de `dotnet test` |
| 2026-09-30 | 68 (todas pasan) | 90,4 % (171 / 189 líneas) | 10,1 % (171 / 1697 líneas, recuento anterior) | ~0,3 s de pruebas |

```bash
dotnet test TXTReader.Tests                        # solo las pruebas
pwsh TXTReader.Tests/cobertura.ps1                 # pruebas + las dos coberturas + tiempo
```

Cómo se cuenta «toda la app» (desde el 2026-10-01): de cada fichero C# que el banco compila, sus
líneas ejecutables según coverlet; de los que no (`Platforms/Android`), todas sus líneas de código
como no cubiertas. Ya no se cuentan llaves, `using` ni firmas, que nunca se ejecutan (con el recuento
nuevo, el 2026-09-30 era el 17,8 %). Queda sin cubrir `MainActivity` y `MainApplication` (48 + 4
líneas: márgenes de las barras del sistema, intents y `ContentResolver`, que solo existen en
Android) y algunos `catch` de `LocalizationService` y `EncodingDetectionService`.

### Pruebas de interfaz (§8.7)

`TXTReader.UITests` (Appium + UiAutomator2, en el emulador MuMu): **4 pruebas E2E de la vista de
Markdown**, ~20 s de tanda (más ~45 s de arranque de Appium). Abren un `.md` de muestra desde otra
app, comprueban que sale renderizado, alternan con el texto tal cual, buscan, siguen un enlace (se
abre fuera) y pasan al tema oscuro. Cómo se lanzan, en [`TXTReader.UITests/README.md`](TXTReader.UITests/README.md).
La tanda mínima del resto de la app (menú, idioma, atrás, letra al 145 %) aún no está.

## 🏗️ Arquitectura Técnica

### 📁 Estructura del Proyecto

```
TXTReader/
├── Services/
│   ├── EncodingDetectionService.cs    # Detección automática de codificación
│   ├── RecentFilesService.cs          # Gestión inteligente de archivos recientes
│   ├── FileIntentService.cs           # Aviso de fichero recibido a la pantalla principal
│   ├── IntentFileHandler.cs           # Apertura de ficheros que llegan de otras apps
│   ├── AppPlatform.cs                 # Puertas a lo que depende del dispositivo
│   ├── MarkdownRenderer.cs            # Markdown a HTML (Markdig), imágenes locales incrustadas
│   ├── ReaderHtml.cs                  # Página del visor: texto o Markdown, tema, búsqueda, sin red
│   ├── ReaderLinks.cs                 # Qué enlaces se abren fuera de la app
│   └── MobileLogService.cs            # Registro de depuración
├── Pages/
│   ├── MainPage.xaml                  # Pantalla principal con historial
│   ├── TextReaderPage.xaml            # Lector con WebView y búsqueda
│   └── AboutPage.xaml                 # Información de la aplicación
├── Platforms/Android/
│   └── MainActivity.cs                # Intents, ContentResolver y barras del sistema
└── Resources/                         # Iconos, colores y recursos
```

### 🔧 Tecnologías Clave

- **.NET MAUI** - Framework multiplataforma moderno
- **WebView con HTML** - Renderizado de texto con selección real
- **Markdig** (BSD-2-Clause) - Markdown a HTML en el dispositivo, sin red
- **ContentResolver** - Acceso nativo a URIs de content de Android
- **Scoped Storage** - Compatibilidad total con Android 10+
- **Material Design** - Interfaz moderna y consistente

### ⚡ Características Técnicas

- **Sin archivos temporales** - Lectura directa desde URIs de content
- **Detección inteligente de codificación** - Análisis de BOM y patrones de bytes
- **Logging integrado** - Sistema de diagnóstico para desarrollo y soporte
- **Gestión de memoria eficiente** - Optimizado para archivos grandes
- **Navegación nativa** - Integración completa con el sistema Android

## 🤝 Contribuir

Las contribuciones son bienvenidas. Por favor:

1. Fork el proyecto
2. Crea una rama para tu feature (`git checkout -b feature/AmazingFeature`)
3. Commit tus cambios (`git commit -m 'Add some AmazingFeature'`)
4. Push a la rama (`git push origin feature/AmazingFeature`)
5. Abre un Pull Request

## 📄 Licencia

Este proyecto está licenciado bajo la Licencia MIT - ver el archivo [LICENSE](LICENSE) para más detalles.

## ⚖️ Disclaimer Legal

Este software se proporciona 'tal como está', sin garantías de ningún tipo, expresas o implícitas. En ningún caso los autores serán responsables de cualquier reclamo, daño u otra responsabilidad. El uso de este software es bajo su propio riesgo.

## 🎯 Casos de Uso

### 👨‍💻 **Para Desarrolladores**
- Leer logs de aplicaciones desde cualquier servicio
- Revisar archivos de configuración en la nube
- Analizar archivos JSON/XML compartidos

### 📊 **Para Profesionales**
- Abrir documentos de texto desde OneDrive corporativo
- Revisar reportes CSV desde Google Drive
- Leer archivos de configuración desde cualquier servicio de almacenamiento

### 🎓 **Para Estudiantes**
- Acceder a apuntes en formato .txt desde la nube
- Leer archivos Markdown de proyectos
- Revisar código fuente compartido

## 👥 Autores

- **Desarrollador Principal** - *Desarrollo completo* - © 2025

## 🙏 Agradecimientos

- Desarrollado con .NET MAUI
- Iconos y diseño inspirados en Material Design
- Gracias a la comunidad de .NET por las herramientas y recursos

---

**TXT Reader** - Haciendo la lectura de archivos de texto simple y elegante ✨
