# Changelog

Todos los cambios relevantes de TXT Reader se documentan en este archivo.

El formato sigue las pautas de la constitucion del proyecto (versionado
sincronizado entre `ApplicationDisplayVersion` y `ApplicationVersion`).

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
