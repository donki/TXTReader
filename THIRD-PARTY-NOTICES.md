# Third-Party Notices

TXT Reader se publica bajo la licencia MIT (ver `LICENSE`). Este documento inventaria
las dependencias de terceros conforme a la seccion 4 de la constitucion del proyecto.

Todo el codigo de la aplicacion es de desarrollo propio. Las unicas dependencias son el
propio framework de interfaz (.NET MAUI) y las APIs del sistema operativo Android, cuyo
uso no contamina la licencia del proyecto.

## Dependencias

| Dependencia | Uso | Licencia | Titular |
|---|---|---|---|
| Microsoft.Maui.Controls | Framework de interfaz y runtime multiplataforma (.NET MAUI). | MIT | Microsoft |
| Microsoft.Extensions.Logging.Debug | Proveedor de logging de depuracion (solo en configuracion Debug). | MIT | Microsoft |

Ambas son componentes del stack .NET / .NET MAUI publicados por Microsoft bajo licencia
**MIT** (licencia permisiva compatible con la del proyecto).

## APIs del sistema operativo

La aplicacion usa APIs de Android (Storage Access Framework, `ContentResolver`, WebView,
selector de archivos del sistema) a traves de .NET MAUI. El uso de APIs del SO no
contamina la licencia del proyecto (seccion 4 de la constitucion).
