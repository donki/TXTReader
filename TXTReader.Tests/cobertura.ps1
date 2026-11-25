# Pruebas con cobertura (constitucion General 8.6). Uso, desde cualquier carpeta:
#   pwsh TXTReader.Tests/cobertura.ps1
# Compila, pasa las pruebas con coverlet, resume con ReportGenerator (dotnet tool local del repo)
# y da las dos cifras: cobertura de lo instrumentado y cobertura sobre toda la app.
#
# Cobertura sobre toda la app: solo codigo ejecutable (General 8.6, 2026-11-25), con el script
# comun Mobile/Shared/cobertura-app.py (lo que el banco no compila, como MainActivity, se estima con
# las mismas reglas y cuenta como no cubierto).
$ErrorActionPreference = 'Stop'
$tests = $PSScriptRoot
$repo = Split-Path $tests
$results = Join-Path $tests 'TestResults'
if (Test-Path $results) { Remove-Item $results -Recurse -Force }

dotnet build $tests -m:1 -nodeReuse:false -p:UseSharedCompilation=false -v:q -nologo | Out-Host
if ($LASTEXITCODE) { exit $LASTEXITCODE }

$watch = [Diagnostics.Stopwatch]::StartNew()
dotnet test $tests --no-build --collect:"XPlat Code Coverage" --results-directory $results -nologo | Out-Host
$code = $LASTEXITCODE
$watch.Stop()
"Tiempo del banco de pruebas (dotnet test --no-build): {0:N1} s" -f $watch.Elapsed.TotalSeconds

$xml = Get-ChildItem $results -Recurse -Filter coverage.cobertura.xml | Select-Object -First 1
Push-Location $repo
try {
    dotnet tool restore | Out-Null
    dotnet reportgenerator "-reports:$($xml.FullName)" "-targetdir:$results/report" -reporttypes:TextSummary | Out-Null
} finally { Pop-Location }
Get-Content "$results/report/Summary.txt" | Select-Object -First 12

python (Join-Path $repo '..' 'Shared' 'cobertura-app.py') --app $repo --report $xml.FullName --defines '' --detalle
exit $code
