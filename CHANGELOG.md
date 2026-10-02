# Changelog

Todos los cambios relevantes de TXT Reader se documentan en este archivo.

El formato sigue las pautas de la constitucion del proyecto (versionado
sincronizado entre `ApplicationDisplayVersion` y `ApplicationVersion`).

## [2026.10.01.0] (versionCode 2026100100)

### Corregido
- **Zoom y espaciado del texto con la app en castellano**: el visor escribía los tamaños para el
  navegador con coma decimal («15,5px», «1,4») y el navegador los descartaba: mover la barra de
  zoom no cambiaba la letra y se perdía el interlineado. Ahora van siempre con punto.
- **Avisos al abrir desde otra app que no se veían nunca**: el de fichero no disponible y el
  consejo de la nube se ponían sobre el menú (Shell) de la ventana, donde el diálogo no se puede
  mostrar. Ahora salen sobre la pantalla que está a la vista.
- **Avisos que salían en castellano con la app en inglés**: el de fichero no disponible al abrirlo
  desde otra app, el de versión nueva disponible y los de Acerca de (correo copiado, error). Ya
  salen en el idioma de la app.

### Cambiado
- Código muerto fuera: el control `HighlightedTextView` (no se usaba; el lector usa
  `SelectableHighlightedTextView`), las pantallas `SplashPage` y `LogViewerPage`, a las que no se
  llegaba desde ningún sitio, y la lectura y el borrado del registro de depuración que solo usaban
  ellas.
- La lógica de abrir ficheros desde otras apps sale de `MainActivity` a `Services/IntentFileHandler.cs`
  y lo que depende del dispositivo (selector, correo, navegador, portapapeles, diálogos, lectura de
  `content://`) pasa por `Services/AppPlatform.cs`, para poder probarlo. La app hace lo mismo.

### Pruebas
- El banco (General §8.6) pasa de 68 a 165 pruebas y ahora recorre también las pantallas con su
  XAML real (principal, lector, Acerca de, menú y botón de atrás), la apertura desde otras apps, la
  comprobación de versión y el visor. Cobertura sobre toda la app: **92,6 %** (antes 10,1 % con el
  recuento anterior, 17,8 % con el nuevo, que ya no cuenta llaves ni `using` como líneas de la app).

*English:* Zoom and line spacing now work with the app in Spanish (the viewer wrote CSS numbers with
a decimal comma); the notices when opening a file from another app are shown again (they were put
on the window's Shell, where the dialog cannot appear), and they, the "update available" notice and
the About notices now follow the app language instead of always being in Spanish. Dead code removed (unused viewer control, splash
and log viewer pages). Tests: 165, 92.6 % line coverage of the whole app.

## [2026.09.30.0] (versionCode 2026093000)

### Corregido
- **Ficheros ANSI de Windows (Windows-1252 / Latin-1)**: un «canción» guardado sin UTF-8 se veía
  como «canci�n», y si el fichero tenía el símbolo € (u otros bytes 0x80-0x9F) pedir esa
  codificación fallaba —.NET no la trae sin registrar su proveedor— y el fichero no se abría desde
  otras apps. Ahora todo lo que no es UTF-8 válido se lee como Windows-1252.
- **Ficheros UTF-32 con marca (BOM)** se leían como UTF-16 y salían ilegibles: la marca de UTF-32 LE
  empieza igual que la de UTF-16 LE y se miraba después.
- **Buscar «&», «<» o comillas** no resaltaba nada, y buscar «amp» o «lt» rompía el texto (se veía
  «&amp;» en vez de «&»): se buscaba en el texto ya convertido a HTML. Ahora se busca en el texto
  original (`Services/TextHighlighter.cs`).
- **Acerca de**: en un móvil en castellano sin idioma elegido, el botón marcado era «English» aunque
  la app se veía en castellano. Y «idioma del sistema» ya no se queda en el último idioma elegido.

### Añadido
- **Pruebas automatizadas** (General §8.6): proyecto `TXTReader.Tests` (xUnit) con la detección de
  codificación, los recientes, el resaltado de la búsqueda y los idiomas. Se ejecutan con
  `dotnet test TXTReader.Tests`.

*English:* ANSI (Windows-1252/Latin-1) files now open with their accents and € sign; UTF-32 files
with a BOM are read correctly; searching for "&", "<" or quotes highlights them and no longer breaks
the text; the About page marks the language actually on screen. Automated tests added
(`dotnet test TXTReader.Tests`).

## [2026.09.29.0] (versionCode 2026092900)

### Cambiado
- **El aviso de «no se puede abrir el fichero» ya no nombra servicios ajenos** (constitución Web §4,
  aplicada también dentro de las apps): el diálogo «Dropbox - Error de acceso» pasa a ser «No se
  puede abrir el fichero» / «El servicio de almacenamiento no ha dado acceso al fichero…».
- **Esos avisos salen en el idioma de la aplicación**: estaban escritos a mano en castellano y ahora
  vienen de los recursos es/en (también los de OneDrive y Google Drive).
- El README tampoco nombra ese servicio.

*English:* the "cannot open the file" dialog no longer names third-party storage services ("The
file cannot be opened" / "The storage service did not grant access to the file…"), and all the
cloud-access dialogs now follow the app language (es/en) instead of being hard-coded in Spanish.

## [2026.09.27.0] (versionCode 2026092700)

### Añadido
- **Un error inesperado ya no cierra la aplicación** (constitución General §6.12): se registra con
  su traza en `crash.log` (carpeta de datos de la app, con tamaño acotado), se avisa en el idioma
  de la aplicación y se sigue. Usa la pieza común `Shared/CrashGuard.cs`.

### Corregido
- **Botón de atrás** (Mobile §7): en Android 16 el «atrás predictivo» hacía que no llegase a la
  aplicación (`enableOnBackInvokedCallback="false"`). Ahora, desde el lector vuelve a la pantalla
  principal; desde «Acerca de» abierta en el menú, a Inicio; con el menú lateral abierto, lo
  cierra; y en la pantalla principal la aplicación se oculta sin cerrarse.

## [2026.09.14.0] (versionCode 2026091400)

Versión que se subió a Play tras la 2026.09.12.0 (pista alpha), sin cambios de funciones.

### Cambiado
- **«Acerca de» sin la tarjeta de Ko-fi**: la constitución prohíbe las secciones de donación en las
  aplicaciones (se quitaron la tarjeta, su manejador y sus textos).

### Interno
- La firma sale solo de `..\Shared\signing.props`: el csproj ya no apunta a un keystore propio que
  podía pisarla.
- El README dice dónde conseguirla (Google Play y releases de GitHub), y ya no promete 10 archivos
  recientes ni un límite de 50 MB: la aplicación guarda 5 y no tiene límite de tamaño.

## [2026.08.01.0] (versionCode 202608010)

### Añadido
- **Se pueden abrir ficheros `.xml` y `.gpx`** (nota de autor del 2026-08-01). Los `.gpx` obligaban
  a añadir `application/octet-stream` al filtro del selector: Android no conoce esa extensión y los
  expone con ese tipo, así que salían en gris. El filtro sigue dejando fuera PDF, imágenes y vídeo,
  que sí tienen MIME propio. También se acepta el intent `application/gpx+xml`.

### Cambiado
- **Pantalla principal sin texto repetido** (nota de autor del 2026-08-01): se quita la cabecera
  «TXT Reader / Lector de archivos de texto» y el rótulo «Abrir archivo». El título de la barra de
  navegación ya identifica la app; eran tres veces lo mismo en la misma pantalla.

### Corregido
- `Resources\AppIcon\play_store_icon.png` regenerado desde los SVG actuales: seguía siendo el
  icono anterior al rediseño índigo del 28-jul.

## [2026.06.26.0] (versionCode 202606260)

### Correcciones
- Selector de idioma en "Acerca de": corregida la reentrancia que podia impedir
  aplicar el cambio de idioma (guarda anti-reentrada y no-op si el idioma no cambia).

### Mejora tecnica
- Constitucion del proyecto añadida como submodulo (`constitution/`) y ampliada:
  despliegue con ensamblados embebidos, versionado por fecha, base es/en y aviso legal.
- Registro de servicios en el contenedor de inyeccion de dependencias.
- Documentacion de justificacion de permisos (`docs/PERMISSIONS.md`).
- Politica de privacidad en ingles (`store-listing/privacy-policy-en.md`).

## [2026.05.17.0] (versionCode 202605170)

### Cambios
- Refactorizacion del manejo de archivos en Android para soporte robusto de
  Content URIs mediante ContentResolver (Scoped Storage).
- Soporte de localizacion (i18n): espanol e ingles con deteccion del idioma del
  sistema y seleccion manual persistida.
- Rediseno del icono de la aplicacion para mayor claridad y estetica moderna.
- Visor de logs de depuracion integrado y mejoras en el manejo de archivos.

### Mejora tecnica
- Adicion de la constitucion del proyecto como submodulo (`constitution/`).
- Documentacion de justificacion de permisos (`docs/PERMISSIONS.md`).
- Politica de privacidad en ingles (`store-listing/privacy-policy-en.md`).
- Registro de servicios en el contenedor de inyeccion de dependencias.

## [2025.10.16.7]

### Cambios
- Controles de resaltado de texto en el lector.
- Mejoras de UX en la pagina del lector de texto.
- Colores principales embebidos directamente en `App.xaml`.
- Filtros de intents de Android migrados a atributos en C#.
- Mejoras en el soporte de formatos de archivo y manejo de intents.

## [Inicial]

### Cambios
- Configuracion inicial del proyecto TXT Reader en .NET MAUI (Android).
- Lectura de archivos de texto con deteccion automatica de codificacion.
- Busqueda en tiempo real con resaltado, control de zoom y archivos recientes.
