$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
$sourcePath = Join-Path $root 'assets\branding\balcao-livre-logo-image.png'
$source = [System.Drawing.Image]::FromFile($sourcePath)

function Write-Icon {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][int]$Size
    )

    $bitmap = New-Object System.Drawing.Bitmap($Size, $Size)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.Clear([System.Drawing.Color]::White)
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.DrawImage($source, 0, 0, $Size, $Size)
        $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

$androidSizes = @{
    'mipmap-mdpi' = 48
    'mipmap-hdpi' = 72
    'mipmap-xhdpi' = 96
    'mipmap-xxhdpi' = 144
    'mipmap-xxxhdpi' = 192
}

foreach ($entry in $androidSizes.GetEnumerator()) {
    Write-Icon -Path (Join-Path $root "android\app\src\main\res\$($entry.Key)\ic_launcher.png") -Size $entry.Value
}

$iosFolder = Join-Path $root 'ios\Runner\Assets.xcassets\AppIcon.appiconset'
Get-ChildItem -LiteralPath $iosFolder -Filter 'Icon-App-*.png' | ForEach-Object {
    if ($_.Name -match 'Icon-App-(?<base>\d+(?:\.\d+)?)x\d+(?:\.\d+)?@(?<scale>\d+)x\.png') {
        $size = [int][Math]::Round([double]$Matches.base * [int]$Matches.scale)
        Write-Icon -Path $_.FullName -Size $size
    }
}

Write-Icon -Path (Join-Path $root 'web\icons\Icon-192.png') -Size 192
Write-Icon -Path (Join-Path $root 'web\icons\Icon-maskable-192.png') -Size 192
Write-Icon -Path (Join-Path $root 'web\icons\Icon-512.png') -Size 512
Write-Icon -Path (Join-Path $root 'web\icons\Icon-maskable-512.png') -Size 512
Write-Icon -Path (Join-Path $root 'web\favicon.png') -Size 32

$source.Dispose()
Write-Host 'Ícones do Agenda Livre gerados para Android, iOS e web.'
