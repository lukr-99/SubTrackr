<#
.SYNOPSIS
  Renders the desktop app icon (desktop/SubTrackr.Desktop/Assets/SubTrackr.ico) from the logo
  contract.

.DESCRIPTION
  Reads contracts/design/logo.json (geometry on a square canvas) and the brand colors of
  contracts/design/tokens.json, draws the mark with WPF at 16, 20, 24, 32, 40, 48, 64 and 256
  pixels, and writes one .ico with a PNG image per size. At 32 pixels and below the bars are
  snapped to whole pixels and drawn square, so the taskbar and title bar icons stay crisp.

  Run it with Windows PowerShell 5.1 (WPF needs a single-threaded apartment, which it has by
  default) after changing logo.json or the brand colors, then commit the .ico.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File tools\render-desktop-icon.ps1
.EXAMPLE
  powershell -ExecutionPolicy Bypass -File tools\render-desktop-icon.ps1 -PngFolder "$env:TEMP\subtrackr-icon"
#>
[CmdletBinding()]
param(
  [string] $OutputPath,
  [string] $PngFolder
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
if (-not $OutputPath) {
  $OutputPath = Join-Path $repositoryRoot 'desktop\SubTrackr.Desktop\Assets\SubTrackr.ico'
}

$sizes = @(16, 20, 24, 32, 40, 48, 64, 256)
$snapUpTo = 32

if ([System.Threading.Thread]::CurrentThread.GetApartmentState() -ne [System.Threading.ApartmentState]::STA) {
  throw 'WPF rendering needs a single-threaded apartment. Run this script with Windows PowerShell 5.1 or pwsh -STA.'
}

Add-Type -AssemblyName PresentationCore, WindowsBase

function Read-Json([string] $path) {
  return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
}

function Round-Half([double] $value) {
  return [Math]::Floor($value + 0.5)
}

function ConvertTo-Color([string] $hex) {
  if ($hex -notmatch '^#[0-9A-Fa-f]{6}$') {
    throw "'$hex' is not a #RRGGBB color."
  }
  return [System.Windows.Media.Color]::FromRgb(
    [Convert]::ToByte($hex.Substring(1, 2), 16),
    [Convert]::ToByte($hex.Substring(3, 2), 16),
    [Convert]::ToByte($hex.Substring(5, 2), 16))
}

$logo = Read-Json (Join-Path $repositoryRoot 'contracts\design\logo.json')
$tokens = Read-Json (Join-Path $repositoryRoot 'contracts\design\tokens.json')
if ($logo.format -ne 'subtrackr-logo' -or $logo.version -ne 1) {
  throw 'contracts/design/logo.json is not version 1 of the SubTrackr logo.'
}

$gradientStart = ConvertTo-Color $tokens.brand.gradientStart
$gradientEnd = ConvertTo-Color $tokens.brand.gradientEnd
$markColor = ConvertTo-Color $tokens.brand.mark
$barHeights = @($logo.barHeights | ForEach-Object { [double] $_ })
$barCount = $barHeights.Count

# The bars for one pixel size: exact scaled geometry, or whole pixels at small sizes. Snapped bars
# keep the logo's layout: a centered row, bars standing on one baseline, the tallest bar's column
# centered vertically.
function Get-Bars([int] $size) {
  $scale = $size / [double] $logo.canvas
  $bars = @()
  if ($size -le $snapUpTo) {
    $width = [Math]::Max(1, (Round-Half ($logo.barWidth * $scale)))
    $gap = [Math]::Max(1, (Round-Half ($logo.barGap * $scale)))
    $heights = @($barHeights | ForEach-Object { [Math]::Max(1, (Round-Half ($_ * $scale))) })
    $tallest = ($heights | Measure-Object -Maximum).Maximum
    $row = ($barCount * $width) + (($barCount - 1) * $gap)
    $left = [Math]::Floor(($size - $row) / 2)
    $baseline = [Math]::Floor(($size - $tallest) / 2) + $tallest
    for ($i = 0; $i -lt $barCount; $i++) {
      $bars += New-Object System.Windows.Rect(($left + $i * ($width + $gap)), ($baseline - $heights[$i]), $width, $heights[$i])
    }
    return @{ Bars = $bars; Radius = 0.0 }
  }

  $row = ($barCount * $logo.barWidth) + (($barCount - 1) * $logo.barGap)
  $left = ($logo.canvas - $row) / 2
  for ($i = 0; $i -lt $barCount; $i++) {
    $x = $left + $i * ($logo.barWidth + $logo.barGap)
    $y = $logo.baseline - $barHeights[$i]
    $bars += New-Object System.Windows.Rect(($x * $scale), ($y * $scale), ($logo.barWidth * $scale), ($barHeights[$i] * $scale))
  }
  return @{ Bars = $bars; Radius = $logo.barCornerRadius * $scale }
}

function New-Frame([int] $size) {
  $scale = $size / [double] $logo.canvas
  $visual = New-Object System.Windows.Media.DrawingVisual
  $context = $visual.RenderOpen()
  try {
    $background = New-Object System.Windows.Media.LinearGradientBrush(
      $gradientStart, $gradientEnd, (New-Object System.Windows.Point(0, 0)), (New-Object System.Windows.Point(1, 1)))
    $corner = $logo.cornerRadius * $scale
    $context.DrawRoundedRectangle($background, $null, (New-Object System.Windows.Rect(0, 0, $size, $size)), $corner, $corner)

    $mark = New-Object System.Windows.Media.SolidColorBrush($markColor)
    $layout = Get-Bars $size
    foreach ($bar in $layout.Bars) {
      $context.DrawRoundedRectangle($mark, $null, $bar, $layout.Radius, $layout.Radius)
    }
  }
  finally {
    $context.Close()
  }

  $bitmap = New-Object System.Windows.Media.Imaging.RenderTargetBitmap(
    $size, $size, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
  $bitmap.Render($visual)

  $encoder = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
  $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
  $stream = New-Object System.IO.MemoryStream
  try {
    $encoder.Save($stream)
    return , $stream.ToArray()
  }
  finally {
    $stream.Dispose()
  }
}

$frames = @()
foreach ($size in $sizes) {
  $png = New-Frame $size
  $frames += , @{ Size = $size; Png = $png }
  if ($PngFolder) {
    [void] (New-Item -ItemType Directory -Force -Path $PngFolder)
    [System.IO.File]::WriteAllBytes((Join-Path $PngFolder "icon-$size.png"), $png)
  }
}

# ICONDIR, one ICONDIRENTRY per frame, then the PNG images. A width or height of 0 means 256.
$output = New-Object System.IO.MemoryStream
$writer = New-Object System.IO.BinaryWriter($output)
try {
  $writer.Write([uint16] 0)
  $writer.Write([uint16] 1)
  $writer.Write([uint16] $frames.Count)
  $offset = 6 + (16 * $frames.Count)
  foreach ($frame in $frames) {
    $dimension = if ($frame.Size -ge 256) { 0 } else { $frame.Size }
    $writer.Write([byte] $dimension)
    $writer.Write([byte] $dimension)
    $writer.Write([byte] 0)
    $writer.Write([byte] 0)
    $writer.Write([uint16] 1)
    $writer.Write([uint16] 32)
    $writer.Write([uint32] $frame.Png.Length)
    $writer.Write([uint32] $offset)
    $offset += $frame.Png.Length
  }
  foreach ($frame in $frames) {
    $writer.Write([byte[]] $frame.Png)
  }
  $writer.Flush()
  [System.IO.File]::WriteAllBytes($OutputPath, $output.ToArray())
}
finally {
  $writer.Dispose()
  $output.Dispose()
}

Write-Host "Wrote $OutputPath ($($sizes -join ', ') px)." -ForegroundColor Green
