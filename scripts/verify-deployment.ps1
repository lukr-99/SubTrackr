<#
.SYNOPSIS
  Read-only checks of an installed SubTrackr: versions, data placement, duplicate rows, and the
  Supabase project's row security.

.DESCRIPTION
  Nothing here changes state. Values such as the sync URL and key are read but never printed.

  - Desktop: the installed version matches Version.props, data.json lives outside the install
    folder and parses, and no two live subscriptions look like duplicates.
  - Supabase (when the desktop has a project configured): the auth endpoint accepts the
    publishable key, and an anonymous read of public.subscriptions is refused, which proves
    migration 0002 (per-user rows) is applied.
  - Android (only with -Serial): the release app's installed version on that one device.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File scripts\verify-deployment.ps1
.EXAMPLE
  powershell -ExecutionPolicy Bypass -File scripts\verify-deployment.ps1 -Serial emulator-5554
#>
[CmdletBinding()]
param(
  [string]$Serial
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$failures = [System.Collections.Generic.List[string]]::new()

function Check([string]$name, [bool]$ok, [string]$detail) {
  $status = if ($ok) { 'PASS' } else { 'FAIL' }
  Write-Host ("[{0}] {1}: {2}" -f $status, $name, $detail)
  if (-not $ok) { $failures.Add($name) }
}

function Text([object]$value) {
  if ($null -eq $value) { return '' }
  return [string]$value
}

function DuplicateGroupCount([object[]]$subscriptions) {
  $live = @($subscriptions | Where-Object { [string]::IsNullOrWhiteSpace((Text $_.deletedAt)) })
  $groups = @($live | Group-Object {
      '{0}|{1}|{2}|{3}' -f (Text $_.name).Trim().ToLowerInvariant(), (Text $_.cost.currency).ToUpperInvariant(),
        (Text $_.cost.minorUnits), (Text $_.billingCycle)
    } | Where-Object Count -gt 1)
  return $groups.Count
}

function StatusCodeOf([scriptblock]$request) {
  try {
    & $request | Out-Null
    return 200
  } catch {
    $response = $_.Exception.Response
    if ($null -ne $response) { return [int]$response.StatusCode }
    return -1
  }
}

[xml]$versionProps = Get-Content -Raw -LiteralPath (Join-Path $repo 'Version.props')
$expectedVersion = @($versionProps.Project.PropertyGroup.SubTrackrVersion | Where-Object { $_ })[0]

$desktopExe = Join-Path $env:LOCALAPPDATA 'Programs\SubTrackr\SubTrackr.exe'
$desktopInstalled = Test-Path -LiteralPath $desktopExe
$desktopVersion = if ($desktopInstalled) {
  (Get-Item -LiteralPath $desktopExe).VersionInfo.FileVersion -replace '\.0$', ''
} else { '<missing>' }
Check 'Desktop installed version' ($desktopInstalled -and $desktopVersion -eq $expectedVersion) "installed=$desktopVersion expected=$expectedVersion"

$desktopDataPath = Join-Path $env:APPDATA 'SubTrackr\data.json'
$desktopDataExists = Test-Path -LiteralPath $desktopDataPath
$outsideInstall = -not $desktopDataPath.StartsWith((Split-Path $desktopExe -Parent), [StringComparison]::OrdinalIgnoreCase)
Check 'Desktop data survives installer updates' ($desktopDataExists -and $outsideInstall) 'data.json exists outside the install folder'

if ($desktopDataExists) {
  $desktopDb = Get-Content -Raw -Encoding UTF8 -LiteralPath $desktopDataPath | ConvertFrom-Json
  $rows = @($desktopDb.subscriptions)
  $duplicates = DuplicateGroupCount $rows
  Check 'Desktop subscription uniqueness' ($duplicates -eq 0) "duplicate groups=$duplicates rows=$($rows.Count)"

  $syncUrl = (Text $desktopDb.settings.syncUrl).TrimEnd('/')
  $syncKey = Text $desktopDb.settings.syncKey
  if ([string]::IsNullOrWhiteSpace($syncUrl) -or [string]::IsNullOrWhiteSpace($syncKey)) {
    Write-Host '[SKIP] Supabase: no project configured on this desktop'
  } else {
    $headers = @{ apikey = $syncKey }
    $authStatus = StatusCodeOf { Invoke-RestMethod -Method Get -Uri "$syncUrl/auth/v1/settings" -Headers $headers -TimeoutSec 15 }
    Check 'Supabase auth reachable' ($authStatus -eq 200) "GET /auth/v1/settings returned $authStatus; -1 means no answer (paused project, DNS, or network). URL and key redacted"

    $anonStatus = StatusCodeOf { Invoke-RestMethod -Method Get -Uri "$syncUrl/rest/v1/subscriptions?select=id&limit=1" -Headers $headers -TimeoutSec 15 }
    Check 'Supabase refuses anonymous reads' ($anonStatus -eq 401 -or $anonStatus -eq 403) "anonymous read returned $anonStatus; 200 means migration 0002 is missing"
  }
}

if ($Serial) {
  $adb = Get-Command adb -ErrorAction SilentlyContinue
  if (-not $adb) {
    Check 'Android tools' $false 'adb is not on PATH'
  } else {
    $state = (& $adb.Source -s $Serial get-state 2>$null) -join ''
    Check 'Android device reachable' ($state -eq 'device') "serial redacted; state=$state"
    if ($state -eq 'device') {
      $packageDump = (& $adb.Source -s $Serial shell dumpsys package com.lukr99.subtrackr 2>$null) -join "`n"
      $phoneVersion = if ($packageDump -match 'versionName=([^\s]+)') { $Matches[1] } else { '<missing>' }
      Check 'Android installed version' ($phoneVersion -eq $expectedVersion) "installed=$phoneVersion expected=$expectedVersion"
    }
  }
} else {
  Write-Host '[SKIP] Android: pass -Serial to check one device'
}

if ($failures.Count -gt 0) {
  Write-Error ("Deployment verification failed: " + ($failures -join ', '))
  exit 1
}

Write-Host "Deployment verification passed for SubTrackr $expectedVersion."
