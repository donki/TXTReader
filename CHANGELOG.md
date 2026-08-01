# Changelog

Todos los cambios relevantes de TXT Reader se documentan en este archivo.

El formato sigue las pautas de la constitucion del proyecto (versionado
sincronizado entre `ApplicationDisplayVersion` y `ApplicationVersion`).

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
