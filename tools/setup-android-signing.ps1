<#
.SYNOPSIS
  Android release signing for SubTrackr: create the release key, back it up, and optionally upload
  the CI secrets.

.DESCRIPTION
  1. Generate android/subtrackr-release.jks with keytool, unless it already exists. The password is
     typed hidden or, with -GeneratePassword, generated and never printed. It reaches keytool only
     through an environment variable.
  2. Write android/keystore.properties (git-ignored, no byte-order mark) so local release builds
     sign.
  3. Copy the keystore into every -BackupTo folder and verify each copy by SHA-256. With
     -IncludePassword the properties file goes along; keep such a folder offline.
  4. With -UploadSecrets, set SUBTRACKR_KEYSTORE_BASE64, SUBTRACKR_KEYSTORE_PASSWORD,
     SUBTRACKR_KEY_ALIAS and SUBTRACKR_KEY_PASSWORD on -Repository through a short-lived env file.

  Losing this key means installed copies can never be updated in place. Keep one backup offline
  and the password in a password manager.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File tools\setup-android-signing.ps1 -GeneratePassword -BackupTo 'E:\keys\subtrackr' -IncludePassword
.EXAMPLE
  powershell -ExecutionPolicy Bypass -File tools\setup-android-signing.ps1 -UploadSecrets
#>
[CmdletBinding()]
param(
  [string]   $KeyAlias = 'subtrackr',
  [int]      $ValidityDays = 10000,
  [string[]] $BackupTo = @(),
  [switch]   $IncludePassword,
  [switch]   $GeneratePassword,
  [string]   $DistinguishedName = 'CN=SubTrackr, O=SubTrackr',
  [switch]   $UploadSecrets,
  [string]   $Repository = 'lukr-99/SubTrackr'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$androidRoot = Join-Path $repositoryRoot 'android'
$keystoreName = 'subtrackr-release.jks'
$keystorePath = Join-Path $androidRoot $keystoreName
$propertiesPath = Join-Path $androidRoot 'keystore.properties'
# UTF-8 without a byte-order mark: java.util.Properties would read a BOM into the first key, and
# Gradle would then build an unsigned release without complaint.
$Utf8NoBom = New-Object System.Text.UTF8Encoding $false

function Find-Keytool {
  $onPath = Get-Command keytool -ErrorAction SilentlyContinue
  if ($onPath) { return $onPath.Source }
  if ($env:JAVA_HOME) {
    $candidate = Join-Path $env:JAVA_HOME 'bin\keytool.exe'
    if (Test-Path -LiteralPath $candidate) { return $candidate }
  }
  throw 'keytool was not found. Put a JDK on PATH or set JAVA_HOME.'
}

function New-RandomPassword {
  $bytes = New-Object byte[] 32
  $generator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
  try { $generator.GetBytes($bytes) } finally { $generator.Dispose() }
  return [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
}

function Read-NewPassword {
  while ($true) {
    $first = Read-Host 'Keystore password (hidden, at least 6 characters)' -AsSecureString
    $second = Read-Host 'Repeat the password' -AsSecureString
    $a = [Runtime.InteropServices.Marshal]::PtrToStringBSTR([Runtime.InteropServices.Marshal]::SecureStringToBSTR($first))
    $b = [Runtime.InteropServices.Marshal]::PtrToStringBSTR([Runtime.InteropServices.Marshal]::SecureStringToBSTR($second))
    if ($a -ne $b) { Write-Host 'The two entries differ. Try again.' -ForegroundColor Yellow; continue }
    if ($a.Length -lt 6) { Write-Host 'keytool needs at least 6 characters. Try again.' -ForegroundColor Yellow; continue }
    return $a
  }
}

function Read-Properties {
  $values = @{}
  Get-Content -LiteralPath $propertiesPath | ForEach-Object {
    if ($_ -match '^\s*([^#=]+)=(.*)$') { $values[$matches[1].Trim()] = $matches[2].Trim() }
  }
  return $values
}

Write-Host '== SubTrackr Android signing setup ==' -ForegroundColor Cyan
$keytool = Find-Keytool
$fingerprint = $null
if (Test-Path -LiteralPath $keystorePath) {
  Write-Host "Keystore already exists at $keystorePath; not generating a new one." -ForegroundColor Yellow
} else {
  if (Test-Path -LiteralPath $propertiesPath) {
    throw "$propertiesPath exists without its keystore. Move it aside first."
  }
  $password = if ($GeneratePassword) { New-RandomPassword } else { Read-NewPassword }

  # PKCS12 keystores ignore a separate key password, so the key uses the store password.
  $env:SUBTRACKR_STOREPASS = $password
  try {
    & $keytool -genkeypair -keystore $keystorePath -alias $KeyAlias -keyalg RSA -keysize 2048 `
      -validity $ValidityDays -storepass:env SUBTRACKR_STOREPASS -keypass:env SUBTRACKR_STOREPASS `
      -dname $DistinguishedName
    if ($LASTEXITCODE -ne 0) { throw "keytool failed with exit code $LASTEXITCODE" }
    $listing = & $keytool -list -v -keystore $keystorePath -alias $KeyAlias -storepass:env SUBTRACKR_STOREPASS
    $fingerprint = ($listing | Select-String -Pattern 'SHA256:\s*(.+)$' | Select-Object -First 1).Matches.Groups[1].Value.Trim()
  } finally {
    Remove-Item Env:SUBTRACKR_STOREPASS -ErrorAction SilentlyContinue
  }

  $properties = "storeFile=$keystoreName`nstorePassword=$password`nkeyAlias=$KeyAlias`nkeyPassword=$password`n"
  [IO.File]::WriteAllText($propertiesPath, $properties, $Utf8NoBom)
  Remove-Variable password, properties
  Write-Host 'Wrote android/keystore.properties (git-ignored; it holds the password).' -ForegroundColor Green
  if ($fingerprint) { Write-Host "Signing certificate SHA-256: $fingerprint" }
}

$keyHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $keystorePath).Hash
$folders = @($BackupTo | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim().Trim("'", '"') } | Where-Object { $_ })
foreach ($folder in $folders) {
  New-Item -ItemType Directory -Force -Path $folder | Out-Null
  $target = Join-Path $folder $keystoreName
  if ((Test-Path -LiteralPath $target) -and ((Get-FileHash -Algorithm SHA256 -LiteralPath $target).Hash -ne $keyHash)) {
    throw "$target already exists and is a DIFFERENT key. Not overwriting it; move it aside first."
  }
  Copy-Item -LiteralPath $keystorePath -Destination $target -Force
  if ((Get-FileHash -Algorithm SHA256 -LiteralPath $target).Hash -ne $keyHash) {
    throw "Backup copy at $target does not match the original."
  }
  if ($IncludePassword) {
    $propertiesTarget = Join-Path $folder 'keystore.properties'
    Copy-Item -LiteralPath $propertiesPath -Destination $propertiesTarget -Force
    if ((Get-FileHash -Algorithm SHA256 -LiteralPath $propertiesTarget).Hash -ne (Get-FileHash -Algorithm SHA256 -LiteralPath $propertiesPath).Hash) {
      throw "Backup copy at $propertiesTarget does not match the original."
    }
  }
  $notes = @(
    'SubTrackr Android release signing key',
    "File: $keystoreName",
    "Key alias: $KeyAlias",
    "File SHA-256: $keyHash",
    "Backed up: $(Get-Date -Format 'yyyy-MM-dd HH:mm')"
  )
  if ($fingerprint) { $notes += "Signing certificate SHA-256: $fingerprint" }
  if ($IncludePassword) {
    $notes += @('', 'keystore.properties next to this file holds the password. Keep this folder offline.')
  } else {
    $notes += @('', 'The password is not stored here. Keep it in a password manager.')
  }
  $notes += 'Losing this key means installed copies of SubTrackr can no longer be updated in place.'
  [IO.File]::WriteAllText((Join-Path $folder 'README.txt'), (($notes -join "`r`n") + "`r`n"), $Utf8NoBom)
  Write-Host "Backed up and verified: $target" -ForegroundColor Green
}
if ($folders.Count -eq 0) {
  Write-Host "No -BackupTo folders given: back up android/$keystoreName yourself, one copy offline." -ForegroundColor Yellow
}

if ($UploadSecrets) {
  if (-not (Get-Command gh -ErrorAction SilentlyContinue)) { throw 'gh CLI not found on PATH.' }
  $props = Read-Properties
  # A short-lived env file, not a pipe (PowerShell would append a newline to the password) and not
  # --body (that would put the password on gh's command line).
  $secrets = [IO.Path]::Combine([IO.Path]::GetTempPath(), "subtrackr-secrets-$([guid]::NewGuid().ToString('N')).env")
  try {
    $b64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes($keystorePath))
    $lines = "SUBTRACKR_KEYSTORE_BASE64=$b64`nSUBTRACKR_KEYSTORE_PASSWORD=$($props['storePassword'])`nSUBTRACKR_KEY_ALIAS=$($props['keyAlias'])`nSUBTRACKR_KEY_PASSWORD=$($props['keyPassword'])`n"
    [IO.File]::WriteAllText($secrets, $lines, $Utf8NoBom)
    & gh secret set --repo $Repository --env-file $secrets
    if ($LASTEXITCODE -ne 0) { throw "gh secret set failed with exit code $LASTEXITCODE" }
  } finally {
    Remove-Item -LiteralPath $secrets -Force -ErrorAction SilentlyContinue
  }
  Write-Host "Uploaded the four SUBTRACKR_KEYSTORE / KEY secrets to $Repository." -ForegroundColor Green
}

Write-Host 'Done. Test a signed build with: android\gradlew.bat -p android assembleRelease' -ForegroundColor Cyan
