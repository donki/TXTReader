# TXT Reader

📖 **Lector de archivos de texto moderno y profesional para Android y Windows**

Una aplicación multiplataforma desarrollada con .NET MAUI que permite leer archivos de texto con detección automática de codificación, búsqueda integrada y una interfaz moderna.

## ✨ Características

- 📁 **Detección automática de codificación** - Soporte para UTF-8, UTF-16, UTF-32, Windows-1252
- 🔍 **Búsqueda de texto integrada** - Encuentra texto rápidamente dentro de los archivos
- 🔍 **Control de zoom avanzado** - Ajusta el tamaño de fuente de 8px a 32px
- 🕒 **Historial de archivos recientes** - Acceso rápido a los últimos 5 archivos abiertos
- 📱 **Apertura de archivos por intents** - Abre archivos desde otras aplicaciones (Android)
- 🎨 **Interfaz moderna** - Diseño profesional con Material Design
- 🚀 **SplashScreen elegante** - Pantalla de carga con branding

## 📱 Plataformas Soportadas

- **Android** (API 21+)
- **Windows** (Windows 10 versión 1809+)

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
- Visual Studio 2022 17.8+ o Visual Studio Code
- Para Android: Android SDK y emulador/dispositivo
- Para Windows: Windows 10 SDK

### Compilación

```bash
# Clonar el repositorio
git clone https://github.com/tu-usuario/TXTReader.git
cd TXTReader

# Restaurar dependencias
dotnet restore

# Compilar para Android
dotnet build -f net10.0-android

# Compilar para Windows
dotnet build -f net10.0-windows10.0.19041.0

# Instalar en dispositivo Android
dotnet build -f net10.0-android -t:Run
```

## 🎯 Uso

### Abrir Archivos

1. **Desde la aplicación**: Usa el botón "📁 Seleccionar Archivo" en la pantalla principal
2. **Desde otras aplicaciones** (Android): Comparte o abre archivos de texto con TXT Reader
3. **Archivos recientes**: Accede rápidamente desde la lista de archivos recientes

### Funciones del Lector

- **Búsqueda**: Usa el campo de búsqueda en la parte superior para encontrar texto
- **Zoom**: Usa los botones A+ y A- para ajustar el tamaño de fuente
- **Navegación**: Desplázate por archivos largos con scroll suave
- **Información**: El título muestra la codificación detectada del archivo

## 🏗️ Arquitectura

```
TXTReader/
├── Services/
│   ├── EncodingDetectionService.cs    # Detección automática de codificación
│   ├── RecentFilesService.cs          # Gestión de archivos recientes
│   └── FileIntentService.cs           # Manejo de intents de archivos
├── Pages/
│   ├── MainPage.xaml                  # Pantalla principal
│   ├── TextReaderPage.xaml            # Lector de texto
│   ├── AboutPage.xaml                 # Información de la aplicación
│   └── SplashPage.xaml                # Pantalla de carga
├── Platforms/
│   ├── Android/                       # Código específico de Android
│   └── Windows/                       # Código específico de Windows
└── Resources/                         # Recursos de la aplicación
```

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

## 👥 Autores

- **Socratic** - *Desarrollo inicial* - © 2024

## 🙏 Agradecimientos

- Desarrollado con .NET MAUI
- Iconos y diseño inspirados en Material Design
- Gracias a la comunidad de .NET por las herramientas y recursos

---

**TXT Reader** - Haciendo la lectura de archivos de texto simple y elegante ✨
