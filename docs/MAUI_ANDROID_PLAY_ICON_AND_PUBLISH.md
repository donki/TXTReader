# MAUI Android: subir icono de Google Play junto al AAB

Esta guia sirve para aplicar en otro proyecto .NET MAUI Android el mismo flujo usado aqui:

- Generar un PNG `512x512` para el icono de ficha de Google Play a partir del SVG de `MauiIcon`.
- Subir ese icono a Play Console usando Android Publisher API.
- Publicar o actualizar un track con un AAB.
- Reutilizar un `versionCode` existente cuando Google Play ya tiene ese AAB subido.

Importante: el icono de launcher de Android y el icono de ficha de Google Play no son lo mismo. MAUI genera `mipmap/appicon` dentro del AAB, pero Play Console no usa automaticamente ese icono como icono de tienda. El icono de tienda se sube como asset de la ficha.

## Requisitos

En el proyecto destino necesitas:

- Un proyecto `.NET MAUI` con target Android.
- Un `MauiIcon` definido en el `.csproj`.
- Un AAB firmado ya generado, normalmente en `bin/Release/netX.0-android/...Signed.aab`.
- Una cuenta de servicio de Google Play Console con permisos suficientes para crear releases y editar la ficha.
- Un JSON de cuenta de servicio.
- PowerShell 7 o superior.
- Workload MAUI/Android instalado.

Ejemplo de `MauiIcon` en el `.csproj`:

```xml
<ItemGroup>
  <MauiIcon Include="Resources\AppIcon\appicon.svg"
            ForegroundFile="Resources\AppIcon\appiconfg.svg"
            Color="#512BD4" />
</ItemGroup>
```

## Estructura recomendada

Usa esta ubicacion para el icono de Play:

```text
Resources/AppIcon/play_store_icon.png
```

El PNG debe cumplir las reglas de Play Console:

- PNG de 32 bits.
- `512x512`.
- Maximo `1024 KB`.
- Sin alpha si quieres evitar problemas visuales; fondo solido recomendado.

## Generar el PNG desde el SVG de MAUI

Este comando usa las librerias que ya instala MAUI Resizetizer en la cache NuGet. Ajusta la version `10.0.20` si el proyecto usa otra.

Ejecutalo desde la raiz del proyecto:

```powershell
$pkg = Join-Path $env:USERPROFILE '.nuget\packages\microsoft.maui.resizetizer\10.0.20\buildTransitive'
$env:PATH = "$pkg\x64;$env:PATH"

Add-Type -Path (Join-Path $pkg 'SkiaSharp.dll')
Add-Type -Path (Join-Path $pkg 'Svg.Model.dll')
Add-Type -Path (Join-Path $pkg 'Svg.Skia.dll')

$source = Join-Path (Get-Location) 'Resources\AppIcon\appicon.svg'
$out = Join-Path (Get-Location) 'Resources\AppIcon\play_store_icon.png'

$svg = [Svg.Skia.SKSvg]::new()
$picture = $svg.Load($source)
if ($null -eq $picture) {
    throw "No se pudo cargar el SVG: $source"
}

$ok = $svg.Save(
    $out,
    [SkiaSharp.SKColors]::White,
    [SkiaSharp.SKEncodedImageFormat]::Png,
    100,
    1.0,
    1.0
)

if (-not $ok) {
    throw "No se pudo guardar el PNG: $out"
}

$bitmap = [SkiaSharp.SKBitmap]::Decode($out)
try {
    [pscustomobject]@{
        Path = $out
        Width = $bitmap.Width
        Height = $bitmap.Height
        Bytes = (Get-Item -LiteralPath $out).Length
    }
}
finally {
    if ($bitmap) {
        $bitmap.Dispose()
    }
}
```

La salida debe indicar `Width = 512` y `Height = 512`.

Si el SVG no se renderiza igual que esperas, abre `Resources/AppIcon/play_store_icon.png` y revisalo visualmente antes de subirlo. Algunos SVG con filtros, fuentes o efectos complejos pueden renderizar diferente segun la herramienta.

## Script de publicacion

El script de publicacion debe hacer estas operaciones en un mismo `edit` de Google Play:

1. Crear un edit.
2. Subir el AAB, o reutilizar un `versionCode` existente.
3. Subir el icono de ficha con `edits.images.upload`.
4. Actualizar el track.
5. Validar o hacer commit.

Endpoint para subir el icono:

```text
POST https://androidpublisher.googleapis.com/upload/androidpublisher/v3/applications/{packageName}/edits/{editId}/listings/{language}/icon
Content-Type: image/png
```

Donde:

- `{packageName}` es el `ApplicationId`, por ejemplo `com.empresa.app`.
- `{editId}` es el id devuelto al crear el edit.
- `{language}` es el idioma de la ficha, por ejemplo `es-ES` o `en-US`.
- `icon` es el `imageType` de Android Publisher API para el icono de alta resolucion.

## Parametros recomendados para el script

Anade estos parametros al `param(...)` del script:

```powershell
[string]$StoreIconPath,
[string]$StoreListingLanguage = 'es-ES',
[switch]$SkipStoreIcon,
[switch]$SkipAabUpload,
[switch]$ReuseExistingVersionCode,
[string]$ExistingVersionCode,
[bool]$ErrorIfInReview = $true,
[bool]$SendForReview = $true,
[switch]$AssumeYes,
```

Define tambien una ruta por defecto:

```powershell
$DefaultStoreIconPath = Join-Path $ProjectRoot 'Resources\AppIcon\play_store_icon.png'
```

## Funcion de upload reutilizable

La funcion de subida debe aceptar `ContentType`, porque el AAB usa `application/octet-stream` y el icono usa `image/png`.

```powershell
function Invoke-GoogleMediaUpload {
    param(
        [Parameter(Mandatory = $true)][string]$Uri,
        [Parameter(Mandatory = $true)][hashtable]$Headers,
        [Parameter(Mandatory = $true)][string]$FilePath,
        [string]$ContentType = 'application/octet-stream'
    )

    try {
        return Invoke-RestMethod `
            -Method Post `
            -Uri $Uri `
            -Headers $Headers `
            -InFile $FilePath `
            -ContentType $ContentType `
            -TimeoutSec 600
    }
    catch {
        throw "Google API upload error: $(Get-GoogleApiErrorText $_)"
    }
}
```

## Resolver el icono de tienda

Despues de resolver el AAB y antes de autenticar o mostrar el resumen:

```powershell
$uploadStoreIcon = $false
if (-not $SkipStoreIcon) {
    if (-not $StoreIconPath) {
        $StoreIconPath = $DefaultStoreIconPath
    }

    $StoreIconPath = Resolve-InputPath $StoreIconPath
    if (Test-Path -LiteralPath $StoreIconPath) {
        $uploadStoreIcon = $true
        $storeIconItem = Get-Item -LiteralPath $StoreIconPath
    }
    else {
        Write-Warning "No existe el icono de ficha de Play Console: $StoreIconPath. Se continuara sin subir icono."
        $StoreIconPath = $null
    }
}
```

En el resumen conviene mostrarlo:

```powershell
if ($uploadStoreIcon) {
    Write-Host "Icono Play: $StoreIconPath"
    Write-Host "Idioma ficha: $StoreListingLanguage"
    Write-Host "Tamano icono: $($storeIconItem.Length) bytes"
}
```

## Subir el icono dentro del edit

Despues de crear el edit y antes del commit:

```powershell
if ($uploadStoreIcon) {
    Write-Section 'Subiendo icono Play Console'
    $escapedStoreListingLanguage = Escape-PathSegment $StoreListingLanguage
    $iconResult = Invoke-GoogleMediaUpload `
        -Uri "$UploadRoot/applications/$escapedPackageName/edits/$editId/listings/$escapedStoreListingLanguage/icon" `
        -Headers $headers `
        -FilePath $StoreIconPath `
        -ContentType 'image/png'

    if ($iconResult.image.id) {
        Write-Host "Icono subido. Id: $($iconResult.image.id)"
    }
    else {
        Write-Host 'Icono subido.'
    }
}
```

## Caso normal: subir AAB nuevo e icono

Si el `versionCode` aun no existe en Google Play:

```powershell
pwsh -NoProfile -File .\publish_aab_to_play.ps1 `
  -PackageName 'com.empresa.app' `
  -AabPath 'bin\Release\net10.0-android\com.empresa.app-Signed.aab' `
  -Track 'internal' `
  -Status completed `
  -ReleaseName 'Mi App 1.0.0 (100)' `
  -ReleaseNotes '' `
  -StoreIconPath 'Resources\AppIcon\play_store_icon.png' `
  -StoreListingLanguage 'es-ES' `
  -ValidateOnly:$false `
  -ErrorIfInReview:$true `
  -SendForReview:$true `
  -AssumeYes
```

## Caso ya subido: reutilizar versionCode existente

Google Play no permite subir dos veces el mismo `versionCode`. Si intentas subir el mismo AAB otra vez, devolvera un error parecido a:

```text
Version code 202605110 has already been used.
```

Eso no se puede forzar. Para actualizar solo el icono o la ficha sin cambiar la version, reutiliza el `versionCode` ya existente y omite la subida del AAB:

```powershell
pwsh -NoProfile -File .\publish_aab_to_play.ps1 `
  -PackageName 'com.empresa.app' `
  -Track 'internal' `
  -Status completed `
  -ReleaseName 'Mi App 1.0.0 (100)' `
  -ReleaseNotes '' `
  -StoreIconPath 'Resources\AppIcon\play_store_icon.png' `
  -StoreListingLanguage 'es-ES' `
  -SkipAabUpload `
  -ExistingVersionCode '100' `
  -ValidateOnly:$false `
  -ErrorIfInReview:$true `
  -SendForReview:$true `
  -AssumeYes
```

Este flujo crea un edit, sube el icono, actualiza el track con el `versionCode` existente y hace commit.

## Corregir rechazo: titulo y descripcion identicos

Si Play Console rechaza la app por `Metadata policy: Identical title and description`, actualiza la ficha con textos distintos. Este repositorio incluye ejemplos en:

- `store-listing/es-ES.json`
- `store-listing/en-US.json`

El JSON debe tener `title`, `shortDescription` y `fullDescription`. Limites habituales de Play Console:

- Titulo: 30 caracteres como maximo.
- Descripcion corta: 80 caracteres como maximo.
- Descripcion completa: 4000 caracteres como maximo.

Ejemplo para validar solo la ficha en espanol sin subir otro AAB:

```powershell
pwsh -NoProfile -File .\publish_aab_to_play.ps1 `
  -PackageName 'com.socratic.txtreader' `
  -Track 'internal' `
  -Status completed `
  -ReleaseName 'TXT Reader 2026.05.17.0 (202605170)' `
  -StoreListingLanguage 'es-ES' `
  -ListingJsonPath 'store-listing\es-ES.json' `
  -SkipAabUpload `
  -ExistingVersionCode '202605170' `
  -SkipStoreIcon `
  -ValidateOnly `
  -AssumeYes
```

Cuando la validacion pase, ejecuta el mismo comando con `-ValidateOnly:$false` y `-SendForReview:$true`.

## Variante: intentar subir y reutilizar si ya existe

Otra opcion es intentar subir el AAB y, si Play responde que el `versionCode` ya existe, reutilizar el version code del proyecto:

```powershell
pwsh -NoProfile -File .\publish_aab_to_play.ps1 `
  -PackageName 'com.empresa.app' `
  -AabPath 'bin\Release\net10.0-android\com.empresa.app-Signed.aab' `
  -Track 'internal' `
  -Status completed `
  -ReleaseName 'Mi App 1.0.0 (100)' `
  -StoreIconPath 'Resources\AppIcon\play_store_icon.png' `
  -StoreListingLanguage 'es-ES' `
  -ReuseExistingVersionCode `
  -ExistingVersionCode '100' `
  -AssumeYes
```

Para implementar esta variante, envuelve la subida del bundle en `try/catch`:

```powershell
try {
    $bundle = Invoke-GoogleMediaUpload `
        -Uri "$UploadRoot/applications/$escapedPackageName/edits/$editId/bundles" `
        -Headers $headers `
        -FilePath $AabPath
    $versionCode = [string]$bundle.versionCode
}
catch {
    $bundleUploadError = Get-GoogleApiErrorText $_
    if ($ReuseExistingVersionCode -and
        $bundleUploadError -match 'Version code .+ has already been used' -and
        -not [string]::IsNullOrWhiteSpace($ExistingVersionCode)) {
        Write-Warning $bundleUploadError
        $versionCode = [string]$ExistingVersionCode
        Write-Host "Se reutilizara VersionCode existente: $versionCode"
    }
    else {
        throw $bundleUploadError
    }
}
```

## Validar sin publicar

Para probar el edit sin hacer commit:

```powershell
pwsh -NoProfile -File .\publish_aab_to_play.ps1 `
  -PackageName 'com.empresa.app' `
  -Track 'internal' `
  -Status completed `
  -StoreIconPath 'Resources\AppIcon\play_store_icon.png' `
  -StoreListingLanguage 'es-ES' `
  -SkipAabUpload `
  -ExistingVersionCode '100' `
  -ValidateOnly `
  -AssumeYes
```

El script debe llamar a:

```text
POST .../edits/{editId}:validate
DELETE .../edits/{editId}
```

Asi se comprueba que Google acepta los cambios y luego se elimina el edit temporal.

## Checklist para otro proyecto

1. Confirmar `ApplicationId`, `ApplicationVersion` y `ApplicationDisplayVersion` en el `.csproj`.
2. Confirmar que `Resources/AppIcon/appicon.svg` existe y se usa en `MauiIcon`.
3. Generar `Resources/AppIcon/play_store_icon.png`.
4. Verificar que el PNG mide `512x512` y pesa menos de `1024 KB`.
5. Copiar o adaptar el script `publish_aab_to_play.ps1`.
6. Confirmar que el JSON de cuenta de servicio no se sube a git.
7. Ejecutar primero con `-ValidateOnly` si es la primera vez.
8. Si el AAB es nuevo, publicar con `-AabPath`.
9. Si el `versionCode` ya existe, publicar con `-SkipAabUpload -ExistingVersionCode`.
10. Revisar Play Console: ficha, icono, track y estado de revision.

## Errores frecuentes

### `Version code ... has already been used`

El AAB ya fue subido antes. No cambies solo el archivo ni lo firmes de nuevo: Google Play identifica el `versionCode`. Soluciones:

- Para publicar una version nueva: incrementa `ApplicationVersion`, reconstruye y firma.
- Para cambiar solo icono/ficha: usa `-SkipAabUpload -ExistingVersionCode`.

### El icono se ve distinto en Play

Revisa el PNG generado, no solo el SVG. Play usa el PNG de ficha. Si hace falta, crea un PNG manual con fondo solido y buen margen visual.

### Google no permite hacer commit por cambios en review

El parametro relevante es `changesInReviewBehavior`.

- `ERROR_IF_IN_REVIEW`: falla si ya hay cambios en revision.
- `CANCEL_IN_REVIEW_AND_SUBMIT`: cancela cambios en revision y envia estos.

En el script se controla con:

```powershell
-ErrorIfInReview:$true
```

o:

```powershell
-ErrorIfInReview:$false
```

### El icono se subio pero no se ve inmediatamente

Play Console puede tardar en refrescar vistas y cache. Revisa tambien que se haya subido al idioma correcto de ficha, por ejemplo `es-ES` frente a `en-US`.

## Seguridad

No subas a git:

- JSON de cuenta de servicio.
- Keystores.
- Archivos de password.
- AAB/APK firmados si no forman parte del flujo del repo.

Ejemplo de `.gitignore`:

```gitignore
bin/
obj/
.vs/
*.tmp

*.keystore
*.jks
*.p12
*.pfx
keystore.password.txt
service-account*.json
```

Si el JSON de cuenta de servicio ya estuvo en el repo, rota la clave desde Google Cloud/Play Console.

## Referencias oficiales

- Android Publisher API `edits.images.upload`: https://developers.google.com/android-publisher/api-ref/rest/v3/edits.images/upload
- Android Publisher API `AppImageType`: https://developers.google.com/android-publisher/api-ref/rest/v3/AppImageType
- Requisitos graficos de Play Console: https://support.google.com/googleplay/android-developer/answer/9866151
