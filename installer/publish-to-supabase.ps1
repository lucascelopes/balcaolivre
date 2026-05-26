param(
    [string]$SupabaseUrl = "https://hzvplpotsdzxygkxrgyi.supabase.co",
    [string]$ServiceRoleKey = $env:SUPABASE_SERVICE_ROLE_KEY,
    [string]$Bucket = "balcao-livre-updates",
    [string]$Version = "1.0.2026"
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ServiceRoleKey)) {
    throw "Defina SUPABASE_SERVICE_ROLE_KEY no ambiente ou passe -ServiceRoleKey. Nao coloque essa chave dentro do app."
}

$root = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")
$installer = Join-Path $root "dist\BalcaoLivrePDV-Setup-$Version.exe"
$manifest = Join-Path $PSScriptRoot "version.json"

if (-not (Test-Path -LiteralPath $installer)) {
    throw "Instalador nao encontrado: $installer"
}

if (-not (Test-Path -LiteralPath $manifest)) {
    throw "Manifesto nao encontrado: $manifest"
}

$headers = @{
    "Authorization" = "Bearer $ServiceRoleKey"
    "apikey" = $ServiceRoleKey
}

$bucketBody = @{
    id = $Bucket
    name = $Bucket
    public = $true
} | ConvertTo-Json

try {
    Invoke-RestMethod -Method Post `
        -Uri "$SupabaseUrl/storage/v1/bucket" `
        -Headers $headers `
        -ContentType "application/json" `
        -Body $bucketBody | Out-Null
    Write-Host "Bucket criado: $Bucket"
}
catch {
    $message = $_.Exception.Message
    if ($message -notmatch "already|exists|409") {
        throw
    }

    Write-Host "Bucket ja existia: $Bucket"
}

function Send-StorageObject {
    param(
        [string]$FilePath,
        [string]$ObjectPath,
        [string]$ContentType
    )

    $uploadHeaders = @{
        "Authorization" = "Bearer $ServiceRoleKey"
        "apikey" = $ServiceRoleKey
        "x-upsert" = "true"
    }

    Invoke-RestMethod -Method Post `
        -Uri "$SupabaseUrl/storage/v1/object/$Bucket/$ObjectPath" `
        -Headers $uploadHeaders `
        -ContentType $ContentType `
        -InFile $FilePath | Out-Null

    Write-Host "Publicado: $ObjectPath"
}

Send-StorageObject -FilePath $installer -ObjectPath "windows/BalcaoLivrePDV-Setup-$Version.exe" -ContentType "application/vnd.microsoft.portable-executable"

# Publique o manifesto por ultimo para o app nunca enxergar uma versao sem instalador disponivel.
Send-StorageObject -FilePath $manifest -ObjectPath "windows/version.json" -ContentType "application/json"

Write-Host "URL do manifesto:"
Write-Host "$SupabaseUrl/storage/v1/object/public/$Bucket/windows/version.json"
