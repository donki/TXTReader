# TXTReader.UITests: pruebas de interfaz (Appium)

Pruebas E2E que manejan la app Android de verdad (constitución General §8.7): Appium (Apache-2.0)
con el driver UiAutomator2, desde xUnit con `Appium.WebDriver` (Apache-2.0). No se ejecutan con la
compilación normal: necesitan el servidor de Appium y un emulador.

## Qué prueba (4 pruebas, en orden sobre la misma sesión)

| Prueba | Qué comprueba |
|---|---|
| T01 | Un `.md` abierto desde otra app («Abrir con», intent `VIEW` con `text/markdown`) sale **renderizado**: el título se ve sin `#`. El botón de la barra (`MarkdownToggle`) enseña el texto tal cual (`# Prueba de Markdown`) y vuelve a la vista renderizada. |
| T02 | Buscar «nota» en la vista renderizada: el visor baja hasta la primera coincidencia (la nota al pie, al final) y el título deja de verse. |
| T03 | Tocar un enlace del documento lo abre **fuera** de la app (otro paquete en primer plano) y se vuelve al lector. |
| T04 | Con el tema del sistema en oscuro (`cmd uimode night yes`) el fondo del documento pasa de claro a oscuro (luminancia de un píxel de la captura en crudo); lo deja en claro al acabar. |

Los textos del documento se buscan dentro del WebView por su accesibilidad (`@text` o
`@content-desc`). La muestra está en `Muestras/` (`prueba.md` con todos los elementos y `logo.png`)
y se copia a `/sdcard/Android/data/com.socratic.txtreader/files/`, que la app lee sin permisos; al
acabar se borra.

Capturas de cada paso en `artifacts/<fecha-hora>/` (ignorada en git).

**Falta** la tanda mínima de §8.7 para el resto de la app (menú, atrás, idioma, letra al 145 %).

## Cómo se lanza

Requisitos (todo en `D:\dev`): Appium en `D:\dev\appium` con el driver `uiautomator2` en
`APPIUM_HOME=D:\dev\appium-home` (ver el README de `TaskManager.UITests`).

1. MuMu en marcha (`127.0.0.1:16416`). Toma el cerrojo `D:\sOCProjects\.mumu.lock` y suéltalo al acabar.
2. La app instalada: APK firmado con la clave compartida (`adb -s 127.0.0.1:16416 install -r <apk>`)
   o pásalo en `TXT_APK`.
3. Ejecuta:

```powershell
dotnet build TXTReader.UITests -m:1 -nodeReuse:false
dotnet test TXTReader.UITests --no-build
```

La prueba arranca sola el servidor de Appium si no hay uno escuchando en el 4723, y lo cierra al
acabar. **Borra los datos de la app** al empezar (`pm clear`, por `noReset=false`): úsala solo en un
emulador o dispositivo de pruebas.

Tiempo: ~20 s de pruebas más ~45 s de arranque de Appium y de la sesión (2026-10-22, 4 de 4).

Variables (todas opcionales):

| Variable | Por defecto | Para qué |
|---|---|---|
| `TXT_UDID` | `127.0.0.1:16416` | Serie adb del dispositivo. |
| `TXT_APPIUM_URL` | `http://127.0.0.1:4723/` | Servidor ya arrancado. |
| `TXT_APPIUM` | `D:\dev\appium\node_modules\.bin\appium.cmd` | Appium para arrancarlo si no responde. |
| `APPIUM_HOME` | `D:\dev\appium-home` | Donde están los drivers. |
| `ANDROID_HOME` | `D:\dev\android-sdk` | SDK con adb. |
| `TXT_APK` | (ninguna) | APK a instalar antes de empezar. |

## Identificadores

`MarkdownToggle` (botón de la cabecera: llega como `content-desc`), `SearchEntry` y `ContentViewer`
(llegan como `resource-id`).
