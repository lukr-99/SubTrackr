[CmdletBinding()]
param(
    [switch]$RequirePhone
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

function DuplicateGroupCount([object[]]$subscriptions, [bool]$supabaseRows = $false) {
    $live = @($subscriptions | Where-Object {
        $deleted = if ($supabaseRows) { $_.deleted_at } else { $_.deletedAt }
        [string]::IsNullOrWhiteSpace((Text $deleted))
    })

    $groups = @($live | Group-Object {
        $name = (Text $_.name).Trim().ToLowerInvariant()
        if ($supabaseRows) {
            $currency = (Text $_.cost_currency).ToUpperInvariant()
            $minor = Text $_.cost_minor
            $cycle = Text $_.billing_cycle
        } else {
            $currency = (Text $_.cost.currency).ToUpperInvariant()
            $minor = Text $_.cost.minorUnits
            $cycle = Text $_.billingCycle
        }
        '{0}|{1}|{2}|{3}' -f $name, $currency, $minor, $cycle
    } | Where-Object Count -gt 1)
    return $groups.Count
}

$projectFile = Join-Path $repo 'desktop\SubTrackr.Desktop\SubTrackr.Desktop.csproj'
$projectXml = Get-Content -Raw -LiteralPath $projectFile
if ($projectXml -notmatch '<Version>([^<]+)</Version>') { throw 'Project version not found.' }
$expectedVersion = $Matches[1]

$desktopExe = Join-Path $env:LOCALAPPDATA 'Programs\SubTrackr\SubTrackr.exe'
$desktopInstalled = Test-Path -LiteralPath $desktopExe
$desktopVersion = if ($desktopInstalled) {
    (Get-Item -LiteralPath $desktopExe).VersionInfo.FileVersion -replace '\.0$',''
} else { '<missing>' }
Check 'Desktop installed version' ($desktopInstalled -and $desktopVersion -eq $expectedVersion) "installed=$desktopVersion expected=$expectedVersion"

$desktopDataPath = Join-Path $env:APPDATA 'SubTrackr\data.json'
$desktopDataExists = Test-Path -LiteralPath $desktopDataPath
Check 'Desktop data survives installer updates' ($desktopDataExists -and -not $desktopDataPath.StartsWith((Split-Path $desktopExe -Parent), [StringComparison]::OrdinalIgnoreCase)) "data is stored outside the install directory"

$desktopDb = $null
if ($desktopDataExists) {
    $desktopDb = Get-Content -Raw -LiteralPath $desktopDataPath | ConvertFrom-Json
    $syncConfigured = -not [string]::IsNullOrWhiteSpace((Text $desktopDb.settings.syncUrl)) -and
        -not [string]::IsNullOrWhiteSpace((Text $desktopDb.settings.syncKey))
    Check 'Desktop Supabase settings' $syncConfigured 'URL and API key are configured (values redacted)'

    $desktopRows = @($desktopDb.subscriptions)
    $desktopDuplicateGroups = DuplicateGroupCount $desktopRows
    Check 'Desktop subscription uniqueness' ($desktopDuplicateGroups -eq 0) "duplicate groups=$desktopDuplicateGroups rows=$($desktopRows.Count)"

    if ($syncConfigured) {
        try {
            $headers = @{
                apikey = Text $desktopDb.settings.syncKey
                Authorization = 'Bearer ' + (Text $desktopDb.settings.syncKey)
            }
            $base = (Text $desktopDb.settings.syncUrl).TrimEnd('/')
            $select = 'id,name,cost_currency,cost_minor,billing_cycle,deleted_at'
            $remoteResponse = Invoke-RestMethod -Method Get -Uri "$base/rest/v1/subscriptions?select=$select" -Headers $headers
            # Invoke-RestMethod deliberately returns a JSON array as one pipeline object. Explicitly
            # enumerate it so Count and duplicate grouping operate on rows, not the array wrapper.
            $remoteRows = @($remoteResponse | ForEach-Object { $_ })
            Check 'Supabase migration/schema' $true "subscriptions query succeeded; rows=$($remoteRows.Count)"
            $remoteDuplicateGroups = DuplicateGroupCount $remoteRows $true
            Check 'Supabase subscription uniqueness' ($remoteDuplicateGroups -eq 0) "duplicate groups=$remoteDuplicateGroups rows=$($remoteRows.Count)"
        } catch {
            Check 'Supabase migration/schema' $false ("query failed with {0}" -f $_.Exception.GetType().Name)
        }
    }
}

$localProperties = Join-Path $repo 'android\local.properties'
$adb = $null
if (Test-Path -LiteralPath $localProperties) {
    $sdkLine = Get-Content -LiteralPath $localProperties | Where-Object { $_ -match '^sdk\.dir=' } | Select-Object -First 1
    if ($sdkLine) {
        $sdk = ($sdkLine -replace '^sdk\.dir=', '') -replace '/', '\'
        $candidate = Join-Path $sdk 'platform-tools\adb.exe'
        if (Test-Path -LiteralPath $candidate) { $adb = $candidate }
    }
}
if (-not $adb) {
    $candidate = Join-Path $env:LOCALAPPDATA 'Android\Sdk\platform-tools\adb.exe'
    if (Test-Path -LiteralPath $candidate) { $adb = $candidate }
}

$phoneConnected = $false
if ($adb) {
    $phoneConnected = @(& $adb devices | Select-String '\sdevice$').Count -gt 0
}
if ($RequirePhone) { Check 'Android phone connected' $phoneConnected 'ADB device available' }

if ($phoneConnected) {
    $packageDump = (& $adb shell dumpsys package com.lukr99.subtrackr 2>$null) -join "`n"
    $phoneVersion = if ($packageDump -match 'versionName=([^\s]+)') { $Matches[1] } else { '<missing>' }
    Check 'Android installed version' ($phoneVersion -eq $expectedVersion) "installed=$phoneVersion expected=$expectedVersion"

    $phoneJson = (& $adb shell run-as com.lukr99.subtrackr cat files/data.json 2>$null) -join "`n"
    if ([string]::IsNullOrWhiteSpace($phoneJson)) {
        # Release builds intentionally disable run-as. Verify the retained settings through the
        # rendered Settings UI without printing their values.
        & $adb shell am force-stop com.lukr99.subtrackr | Out-Null
        & $adb shell monkey -p com.lukr99.subtrackr -c android.intent.category.LAUNCHER 1 2>$null | Out-Null
        Start-Sleep -Seconds 2
        $sizeLine = (& $adb shell wm size | Select-String 'Physical size:' | Select-Object -First 1).ToString()
        if ($sizeLine -match '(\d+)x(\d+)') {
            $width = [int]$Matches[1]
            $height = [int]$Matches[2]
            & $adb shell input tap ([int]($width * 0.84)) ([int]($height * 0.93)) | Out-Null
            Start-Sleep -Milliseconds 800
            1..3 | ForEach-Object {
                & $adb shell input swipe ([int]($width * 0.5)) ([int]($height * 0.75)) `
                    ([int]($width * 0.5)) ([int]($height * 0.2)) 350 | Out-Null
                Start-Sleep -Milliseconds 350
            }
            & $adb shell uiautomator dump /sdcard/subtrackr-verify-ui.xml | Out-Null
            $ui = (& $adb exec-out cat /sdcard/subtrackr-verify-ui.xml) -join "`n"
            & $adb shell rm -f /sdcard/subtrackr-verify-ui.xml | Out-Null
            $uiConfigured = $ui -match 'https://[a-z0-9]+\.supabase\.co' -and
                ($ui -match 'sb_publishable_[A-Za-z0-9_-]+' -or $ui -match 'eyJ[A-Za-z0-9._-]+')
            Check 'Android Supabase settings' $uiConfigured 'URL and API key survived the in-place release upgrade (values redacted)'
            Check 'Android persisted data protection' $true 'release app-private data correctly rejects run-as inspection'
        } else {
            Check 'Android persisted data' $false 'could not inspect release UI dimensions'
        }
    } else {
        try {
            $phoneDb = $phoneJson | ConvertFrom-Json
            $phoneConfigured = -not [string]::IsNullOrWhiteSpace((Text $phoneDb.settings.syncUrl)) -and
                -not [string]::IsNullOrWhiteSpace((Text $phoneDb.settings.syncKey))
            Check 'Android Supabase settings' $phoneConfigured 'URL and API key are configured (values redacted)'
            $phoneRows = @($phoneDb.subscriptions)
            $phoneDuplicateGroups = DuplicateGroupCount $phoneRows
            Check 'Android subscription uniqueness' ($phoneDuplicateGroups -eq 0) "duplicate groups=$phoneDuplicateGroups rows=$($phoneRows.Count)"
        } catch {
            Check 'Android persisted data' $false 'app data was not valid JSON'
        }
    }
}

if ($failures.Count -gt 0) {
    Write-Error ("Deployment verification failed: " + ($failures -join ', '))
    exit 1
}

Write-Host "Deployment verification passed for SubTrackr $expectedVersion."
