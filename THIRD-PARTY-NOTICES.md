# Third-Party Notices

TXT Reader se publica bajo la licencia MIT (ver `LICENSE`). Este documento inventaria
las dependencias de terceros conforme a la seccion 4 de la constitucion del proyecto.

Todo el codigo de la aplicacion es de desarrollo propio. Las dependencias son el propio
framework de interfaz (.NET MAUI), la libreria Markdig para la vista de Markdown y las APIs
del sistema operativo Android, cuyo uso no contamina la licencia del proyecto.

## Dependencias

| Dependencia | Uso | Licencia | Titular |
|---|---|---|---|
| Microsoft.Maui.Controls | Framework de interfaz y runtime multiplataforma (.NET MAUI). | MIT | Microsoft |
| Microsoft.Extensions.Logging.Debug | Proveedor de logging de depuracion (solo en configuracion Debug). | MIT | Microsoft |
| Markdig 1.4.0 | Convierte Markdown a HTML para la vista renderizada (desde 2026.10.22.0). Todo en el dispositivo, sin red. | BSD-2-Clause | Alexandre Mutel |

Las dos primeras son componentes del stack .NET / .NET MAUI publicados por Microsoft bajo
licencia **MIT**. Markdig (https://github.com/xoofx/markdig) se publica bajo **BSD-2-Clause**,
licencia permisiva compatible con MIT y con el uso comercial; exige conservar su aviso de
copyright y su licencia, que se reproducen a continuacion.

### Licencia de Markdig

```
Copyright (c) 2016-2026, Alexandre Mutel
All rights reserved.

Redistribution and use in source and binary forms, with or without modification
, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
```

Solo para las pruebas (no se distribuyen): xunit y su runner, Appium.WebDriver y
Selenium.WebDriver (Apache-2.0), Microsoft.NET.Test.Sdk y coverlet (MIT).

## APIs del sistema operativo

La aplicacion usa APIs de Android (Storage Access Framework, `ContentResolver`, WebView,
selector de archivos del sistema) a traves de .NET MAUI. El uso de APIs del SO no
contamina la licencia del proyecto (seccion 4 de la constitucion).
