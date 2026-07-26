param(
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
$webBuild = Join-Path $projectRoot 'build\web'
$cleanupWorker = Join-Path $projectRoot 'web\legacy_flutter_service_worker_cleanup.js'
$workerEntry = Join-Path $projectRoot 'worker\index.js'
$distRoot = Join-Path $projectRoot 'dist'
$clientDist = Join-Path $distRoot 'client'
$serverDist = Join-Path $distRoot 'server'

Push-Location $projectRoot
try {
    if (-not $SkipTests) {
        & flutter test
        if ($LASTEXITCODE -ne 0) {
            throw "Os testes Flutter falharam com codigo $LASTEXITCODE."
        }
    }

    & flutter build web --release --pwa-strategy=none --no-wasm-dry-run
    if ($LASTEXITCODE -ne 0) {
        throw "O build Flutter Web falhou com codigo $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}

if (-not (Test-Path -LiteralPath (Join-Path $webBuild 'index.html'))) {
    throw 'O build Flutter Web nao gerou index.html.'
}

if (-not (Test-Path -LiteralPath $cleanupWorker)) {
    throw 'Worker de limpeza do cache Flutter nao encontrado.'
}

if (-not (Test-Path -LiteralPath $workerEntry)) {
    throw 'Worker de hospedagem nao encontrado.'
}

Copy-Item -LiteralPath $cleanupWorker `
    -Destination (Join-Path $webBuild 'flutter_service_worker.js') -Force

$mainBundle = Join-Path $webBuild 'main.dart.js'
$bundleSource = [System.IO.File]::ReadAllText($mainBundle)
$forbiddenMarkers = @(
    'Lucas Barbearia',
    'Lucas Cesar Lopes',
    'agenda_livre.data.v1'
)
$requiredMarkers = @(
    'agenda_livre.auth.session.v2',
    'agenda_livre.auth.session.v1',
    'agenda_livre.data.v2.',
    'session_identity_mismatch'
)

foreach ($marker in $forbiddenMarkers) {
    if ($bundleSource.Contains($marker)) {
        throw "Bundle Web rejeitado: marcador legado encontrado ($marker)."
    }
}

foreach ($marker in $requiredMarkers) {
    if (-not $bundleSource.Contains($marker)) {
        throw "Bundle Web rejeitado: marcador obrigatorio ausente ($marker)."
    }
}

foreach ($target in @($clientDist, $serverDist)) {
    if (Test-Path -LiteralPath $target) {
        $resolved = (Get-Item -LiteralPath $target -Force).FullName
        if (-not $resolved.StartsWith($distRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Destino invalido: $resolved"
        }
        [System.IO.Directory]::Delete($resolved, $true)
    }
}

New-Item -ItemType Directory -Path $clientDist, $serverDist -Force | Out-Null
Get-ChildItem -LiteralPath $webBuild -Force |
    Copy-Item -Destination $clientDist -Recurse -Force
Copy-Item -LiteralPath $workerEntry -Destination (Join-Path $serverDist 'index.js') -Force

$required = @(
    (Join-Path $clientDist 'index.html'),
    (Join-Path $clientDist 'main.dart.js'),
    (Join-Path $clientDist 'flutter_service_worker.js'),
    (Join-Path $serverDist 'index.js')
)

foreach ($path in $required) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Arquivo obrigatorio ausente: $path"
    }
}

Write-Output $distRoot
