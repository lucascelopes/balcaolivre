param(
    [string]$ZoneId = ""
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$wranglerConfig = Join-Path $projectRoot "dist\server\wrangler.json"

if (-not (Test-Path -LiteralPath $wranglerConfig)) {
    throw "Build do Cloudflare não encontrado. Execute npm.cmd run build antes desta configuração."
}

if ([string]::IsNullOrWhiteSpace($ZoneId)) {
    $ZoneId = Read-Host "Zone ID de minhaagendalivre.com.br"
}
$ZoneId = $ZoneId.Trim()
if ($ZoneId -notmatch '^[a-fA-F0-9]{32}$') {
    throw "Zone ID inválido. Use os 32 caracteres exibidos no painel da zona Cloudflare."
}

$secureToken = Read-Host "API Token com SSL and Certificates Write" -AsSecureString
$tokenPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureToken)

Push-Location $projectRoot
try {
    $plainToken = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($tokenPointer)
    if ([string]::IsNullOrWhiteSpace($plainToken)) {
        throw "O API Token não pode ficar vazio."
    }

    $ZoneId | & npx.cmd wrangler secret put CLOUDFLARE_SAAS_ZONE_ID --config $wranglerConfig
    if ($LASTEXITCODE -ne 0) {
        throw "Falha ao salvar CLOUDFLARE_SAAS_ZONE_ID no Worker."
    }

    $plainToken | & npx.cmd wrangler secret put CLOUDFLARE_SAAS_API_TOKEN --config $wranglerConfig
    if ($LASTEXITCODE -ne 0) {
        throw "Falha ao salvar CLOUDFLARE_SAAS_API_TOKEN no Worker."
    }

    & npx.cmd wrangler secret list --config $wranglerConfig
    if ($LASTEXITCODE -ne 0) {
        throw "Os segredos foram enviados, mas não foi possível confirmar a lista do Worker."
    }

    Write-Host "Cloudflare for SaaS configurado. O Worker criará o fallback e os certificados na próxima publicação." -ForegroundColor Green
}
finally {
    if ($null -ne $plainToken) {
        $plainToken = $null
    }
    if ($tokenPointer -ne [IntPtr]::Zero) {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($tokenPointer)
    }
    Pop-Location
}
