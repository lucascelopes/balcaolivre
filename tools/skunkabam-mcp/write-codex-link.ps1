param(
    [string]$SupabaseUrl = "https://hzvplpotsdzxygkxrgyi.supabase.co",
    [string]$DeviceId = "",
    [string]$DeviceSecret = "",
    [string]$MachineCode = $env:COMPUTERNAME,
    [string]$StoreName = "SkunKabam"
)

$targetDir = Join-Path $env:APPDATA "SkunKabam"
$targetFile = Join-Path $targetDir "codex-link.json"

New-Item -ItemType Directory -Force -Path $targetDir | Out-Null

if ([string]::IsNullOrWhiteSpace($DeviceId)) {
    $rawDeviceId = "$env:USERNAME-$env:COMPUTERNAME".ToLowerInvariant()
    $DeviceId = ($rawDeviceId -replace '[^a-z0-9._-]+', '-').Trim('-')
}

if ([string]::IsNullOrWhiteSpace($DeviceSecret)) {
    $bytes = New-Object byte[] 32
    $rng = [System.Security.Cryptography.RNGCryptoServiceProvider]::new()
    try {
        $rng.GetBytes($bytes)
    }
    finally {
        $rng.Dispose()
    }
    $DeviceSecret = [Convert]::ToBase64String($bytes).TrimEnd('=') -replace '\+', '-' -replace '/', '_'
}

$payload = [ordered]@{
    supabaseUrl = $SupabaseUrl
    deviceId = $DeviceId
    deviceSecret = $DeviceSecret
    machineCode = $MachineCode
    storeName = $StoreName
}

$json = $payload | ConvertTo-Json -Depth 4
$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText($targetFile, $json, $utf8NoBom)
Write-Host "SkunKabam Codex link salvo em $targetFile"
Write-Host "Configure o mesmo segredo no Supabase:"
Write-Host "npx --yes supabase secrets set SKUN_KABAM_CODEX_DEVICE_SECRET=`"$DeviceSecret`" --project-ref hzvplpotsdzxygkxrgyi"
