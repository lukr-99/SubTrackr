<#
  Build a debug APK, install it without clearing data, and optionally launch it.
  Adapted from the guarded ADB workflows in workout-tracker and ring-set. Defaults to SubTrackr's
  debug build, which installs beside the released app.
#>
param(
    [string]$Serial,
    [string]$PackageName = 'com.lukr99.subtrackr.dev',
    [string]$Module = 'app',
    [switch]$Launch,
    # The Gradle project root; defaults to the repository that contains this tools folder.
    [string]$ProjectRoot
)

$ErrorActionPreference = 'Stop'
# Defaults that use $PSScriptRoot are resolved here, not in param(): Windows PowerShell 5.1 leaves
# $PSScriptRoot empty inside parameter defaults of an advanced script started with `powershell -File`.
if (-not $ProjectRoot) { $ProjectRoot = Join-Path $PSScriptRoot '..' }
. "$PSScriptRoot\common.ps1"

$repo = Resolve-Path -LiteralPath $ProjectRoot
$gradleWrapper = Join-Path $repo 'gradlew.bat'
if (-not (Test-Path -LiteralPath $gradleWrapper)) {
    throw "Gradle wrapper not found at $gradleWrapper"
}

$adb = Get-AdbPath
$device = Get-AuthorizedDeviceSerial -Adb $adb -RequestedSerial $Serial

& $gradleWrapper -p $repo ":${Module}:assembleDebug" --console=plain
if ($LASTEXITCODE -ne 0) {
    throw 'Gradle build failed.'
}

$apk = Join-Path $repo "$Module\build\outputs\apk\debug\$Module-debug.apk"
if (-not (Test-Path -LiteralPath $apk)) {
    throw "Expected APK was not produced: $apk"
}

& $adb -s $device install -r $apk
if ($LASTEXITCODE -ne 0) {
    throw 'ADB install failed. Existing data was not intentionally cleared.'
}

if ($Launch) {
    Start-AppPackage -Adb $adb -Serial $device -PackageName $PackageName
}

Write-Host "Installed $apk on $device." -ForegroundColor Green
