param(
    [string]$ApkPath,
    [string]$AdbPath,
    [string]$Device,
    [switch]$SkipBuild,
    [switch]$NoLaunch
)

$ErrorActionPreference = 'Stop'

$DotnetPath = 'C:\Program Files\dotnet\dotnet.exe'
$ProjectRoot = $PSScriptRoot
$ProjectPath = Join-Path $ProjectRoot 'TXTReader.csproj'
$DefaultApkPath = Join-Path $ProjectRoot 'bin\Debug\net10.0-android\com.socratic.txtreader.apk'
$PackageName = 'com.socratic.txtreader'

function Exit-WithError {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    Write-Host "ERROR: $Message" -ForegroundColor Red
    exit 1
}

function Invoke-NativeCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ErrorMessage,

        [Parameter(Mandatory = $true)]
        [string]$FilePath,

        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        Exit-WithError $ErrorMessage
    }
}

function Find-AdbPath {
    if ($AdbPath) {
        if (Test-Path -LiteralPath $AdbPath) {
            return (Resolve-Path -LiteralPath $AdbPath).Path
        }

        Exit-WithError "No existe el adb indicado: $AdbPath"
    }

    $candidates = @(
        'C:\Program Files\Netease\MuMuPlayerGlobal-12.0\shell\adb.exe',
        'C:\Program Files\Netease\MuMu Player 12\shell\adb.exe',
        'C:\Program Files\MuMu\emulator\nemu\vmonitor\bin\adb_server.exe',
        'C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe',
        'C:\Program Files\Android\android-sdk\platform-tools\adb.exe',
        "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe"
    )

    $fromPath = Get-Command adb.exe -ErrorAction SilentlyContinue
    if ($fromPath) {
        $candidates += $fromPath.Source
    }

    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path -LiteralPath $candidate)) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }

    Exit-WithError 'No se encontro adb.exe. Abre MuMu o instala Android SDK Platform Tools, o usa -AdbPath "ruta\adb.exe".'
}

function Get-ConnectedDevices {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResolvedAdbPath
    )

    $output = & $ResolvedAdbPath devices
    if ($LASTEXITCODE -ne 0) {
        Exit-WithError 'No se pudo consultar la lista de dispositivos adb'
    }

    return @(
        $output |
            Select-Object -Skip 1 |
            Where-Object { $_ -match '\tdevice$' } |
            ForEach-Object { ($_ -split '\s+')[0] }
    )
}

Write-Host '========================================'
Write-Host ' TXT Reader - Instalar en emulador MuMu'
Write-Host '========================================'
Write-Host

Push-Location $ProjectRoot
try {
    if (-not $SkipBuild) {
        Write-Host '[1/4] Compilando APK Debug...'
        Invoke-NativeCommand 'Fallo al compilar el APK' $DotnetPath @('build', $ProjectPath, '-f', 'net10.0-android', '-c', 'Debug')
    }
    else {
        Write-Host '[1/4] Saltando compilacion por parametro -SkipBuild'
    }

    if (-not $ApkPath) {
        $ApkPath = $DefaultApkPath
    }

    if (-not (Test-Path -LiteralPath $ApkPath)) {
        Exit-WithError "APK no encontrado: $ApkPath"
    }

    $resolvedApkPath = (Resolve-Path -LiteralPath $ApkPath).Path
    $resolvedAdbPath = Find-AdbPath

    Write-Host
    Write-Host "[2/4] Usando adb: $resolvedAdbPath"
    Invoke-NativeCommand 'No se pudo iniciar adb' $resolvedAdbPath @('start-server')

    if (-not $Device) {
        $mumuPorts = @('127.0.0.1:7555', '127.0.0.1:16384', '127.0.0.1:16416', '127.0.0.1:16512', '127.0.0.1:62001')
        foreach ($port in $mumuPorts) {
            & $resolvedAdbPath connect $port | Out-Null
        }

        $devices = Get-ConnectedDevices $resolvedAdbPath
        if ($devices.Count -eq 0) {
            Exit-WithError 'No hay ningun emulador conectado. Abre MuMu y vuelve a ejecutar el script.'
        }

        if ($devices.Count -gt 1) {
            Write-Host 'Dispositivos disponibles:'
            $devices | ForEach-Object { Write-Host "  $_" }
            Exit-WithError 'Hay varios dispositivos adb. Ejecuta otra vez indicando -Device "id_del_dispositivo".'
        }

        $Device = $devices[0]
    }

    Write-Host "Dispositivo seleccionado: $Device"

    Write-Host
    Write-Host '[3/4] Instalando APK en MuMu...'
    Invoke-NativeCommand 'Fallo al instalar el APK en MuMu' $resolvedAdbPath @('-s', $Device, 'install', '-r', '-d', $resolvedApkPath)

    Write-Host
    if ($NoLaunch) {
        Write-Host '[4/4] Instalacion completada. No se abre la app por parametro -NoLaunch.'
    }
    else {
        Write-Host '[4/4] Abriendo la app...'
        Invoke-NativeCommand 'La app se instalo, pero no se pudo abrir automaticamente' $resolvedAdbPath @('-s', $Device, 'shell', 'monkey', '-p', $PackageName, '-c', 'android.intent.category.LAUNCHER', '1')
    }

    Write-Host
    Write-Host '========================================'
    Write-Host '      INSTALACION COMPLETADA'
    Write-Host '========================================'
    Write-Host "APK instalado: $resolvedApkPath"
}
finally {
    Pop-Location
}
