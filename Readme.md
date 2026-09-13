# TXT Reader

![.NET MAUI](https://img.shields.io/badge/.NET%20MAUI-10.0-blue)
![Android](https://img.shields.io/badge/Android-5.0%2B-green)
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

### ☁️ **Compatibilidad Total con la Nube**
- 🌐 **OneDrive** - Funciona como Microsoft Edge, sin archivos temporales
- 📱 **Google Drive** - Acceso directo a archivos en la nube
- 📦 **Dropbox** - Soporte completo para archivos compartidos
- 🔗 **URIs de Content** - Lectura nativa usando ContentResolver de Android

### 🚀 **Funcionalidades Avanzadas**
- 🕒 **Historial inteligente** - Últimos 10 archivos con limpieza automática
- 📱 **Intents de Android** - Abre archivos .log desde otras aplicaciones
- 🛡️ **Límite de seguridad** - Protección contra archivos > 50MB
- 🐛 **Sistema de logging** - Diagnóstico integrado para desarrollo
- 🎨 **Interfaz moderna** - Colores azules unificados y navegación nativa

## 📱 Plataformas Soportadas

- **Android** (API 21+ / Android 5.0+)
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
   - Dropbox: Exportar → TXT Reader
3. **Desde otras aplicaciones**: Abre archivos .log, .txt desde cualquier app
4. **Archivos recientes**: Lista inteligente con los últimos 10 archivos

### 🔍 Funciones del Lector

- **Búsqueda en tiempo real**: Escribe en el campo superior, resaltado automático
- **Zoom con barra**: Desliza la barra vertical para ajustar de 8px a 32px
- **Selección de texto**: Mantén presionado para seleccionar y copiar
- **Navegación nativa**: Botón "atrás" de Android para regresar
- **Información técnica**: Codificación detectada en el título

### 🐛 Debug y Logs

- **Acceso a logs**: Botón "Debug Logs" en la pantalla principal
- **Limpiar logs**: Botón "Limpiar" para reiniciar el registro
- **Diagnóstico**: Información detallada para resolución de problemas

## 🏗️ Arquitectura Técnica

### 📁 Estructura del Proyecto

```
TXTReader/
├── Services/
│   ├── EncodingDetectionService.cs    # Detección automática de codificación
│   ├── RecentFilesService.cs          # Gestión inteligente de archivos recientes
│   ├── FileIntentService.cs           # Manejo de intents de Android
│   └── MobileLogService.cs            # Sistema de logging integrado
├── Pages/
│   ├── MainPage.xaml                  # Pantalla principal con historial
│   ├── TextReaderPage.xaml            # Lector con WebView y búsqueda
│   ├── AboutPage.xaml                 # Información de la aplicación
│   ├── LogViewerPage.xaml             # Visor de logs de diagnóstico
│   └── SplashPage.xaml                # Pantalla de carga elegante
├── Platforms/Android/
│   └── MainActivity.cs                # Manejo de URIs de content y intents
└── Resources/                         # Iconos, colores y recursos
```

### 🔧 Tecnologías Clave

- **.NET MAUI** - Framework multiplataforma moderno
- **WebView con HTML** - Renderizado de texto con selección real
- **ContentResolver** - Acceso nativo a URIs de content de Android
- **Scoped Storage** - Compatibilidad total con Android 10+
- **Material Design** - Interfaz moderna y consistente

### ⚡ Características Técnicas

- **Sin archivos temporales** - Lectura directa desde URIs de content
- **Detección inteligente de codificación** - Análisis de BOM y patrones de bytes
- **Límite de seguridad** - Protección automática contra archivos > 50MB
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
- Leer archivos de configuración desde Dropbox

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
