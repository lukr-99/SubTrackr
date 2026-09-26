Set-StrictMode -Version Latest

function Get-AdbPath {
    if ($env:ANDROID_HOME) {
        $candidate = Join-Path $env:ANDROID_HOME 'platform-tools\adb.exe'
        if (Test-Path -LiteralPath $candidate) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }

    $command = Get-Command adb -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    throw 'adb was not found. Set ANDROID_HOME or add platform-tools to PATH.'
}

function Get-AndroidSdkRoot {
    foreach ($candidate in @($env:ANDROID_HOME, $env:ANDROID_SDK_ROOT)) {
        if ($candidate -and (Test-Path -LiteralPath $candidate)) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }
    $adb = Get-Command adb -ErrorAction SilentlyContinue
    if ($adb) {
        return Split-Path -Parent (Split-Path -Parent $adb.Source)
    }
    throw 'Android SDK not found. Set ANDROID_HOME (see docs: android-agent-workflow).'
}

function Get-EmulatorPath {
    $sdk = Get-AndroidSdkRoot
    $candidate = Join-Path $sdk 'emulator\emulator.exe'
    if (-not (Test-Path -LiteralPath $candidate)) {
        $candidate = Join-Path $sdk 'emulator/emulator'
    }
    if (-not (Test-Path -LiteralPath $candidate)) {
        throw "Android Emulator is not installed under $sdk. Run: android sdk install emulator"
    }
    return (Resolve-Path -LiteralPath $candidate).Path
}

function Resolve-OutputFile {
    param(
        [Parameter(Mandatory)]
        [string]$Path
    )

    # Resolve against the PowerShell location; [IO.Path]::GetFullPath uses the process directory.
    $full = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Path)
    [System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($full)) | Out-Null
    return $full
}

function Invoke-NativeCommand {
    <#
      Run a native tool and return its text output with stderr merged. Windows PowerShell turns
      redirected native stderr into terminating errors under 'Stop' (adb, monkey, and emulator all
      write progress there), so the preference is relaxed locally and the exit code is checked
      explicitly instead.
    #>
    param(
        [Parameter(Mandatory)]
        [string]$FilePath,
        [string[]]$Arguments = @(),
        # When set, a nonzero exit code throws this message plus the last lines of output.
        [string]$FailureMessage
    )

    $ErrorActionPreference = 'Continue'
    $output = @(& $FilePath @Arguments 2>&1 | ForEach-Object { "$_" })
    $exitCode = $LASTEXITCODE
    if ($FailureMessage -and $exitCode -ne 0) {
        $tail = @($output | Where-Object { $_.Trim() } | Select-Object -Last 3) -join ' | '
        throw "$FailureMessage (exit $exitCode): $tail"
    }
    return $output
}

function Start-AppPackage {
    param(
        [Parameter(Mandatory)]
        [string]$Adb,
        [Parameter(Mandatory)]
        [string]$Serial,
        [Parameter(Mandatory)]
        [string]$PackageName
    )

    Invoke-NativeCommand -FilePath $Adb `
        -Arguments @('-s', $Serial, 'shell', 'monkey', '-p', $PackageName, '-c', 'android.intent.category.LAUNCHER', '1') `
        -FailureMessage "Failed to launch $PackageName on $Serial; is it installed with a launcher activity?" | Out-Null
}

function Save-DeviceFile {
    <#
      Run a device shell command that writes a unique file under /data/local/tmp, pull it
      byte-safely, and always remove the device-side copy. Binary output is never streamed through
      the PowerShell pipeline because Windows PowerShell redirection corrupts it.
    #>
    param(
        [Parameter(Mandatory)]
        [string]$Adb,
        [Parameter(Mandatory)]
        [string]$Serial,
        [Parameter(Mandatory)]
        [ValidateSet('png', 'xml')]
        [string]$Extension,
        # Shell command words; the literal '{remote}' is replaced by the device-side path.
        [Parameter(Mandatory)]
        [string[]]$ShellCommand,
        [Parameter(Mandatory)]
        [string]$FailureMessage,
        [Parameter(Mandatory)]
        [string]$Output
    )

    $outputPath = Resolve-OutputFile -Path $Output
    $remote = "/data/local/tmp/codeprint-$([guid]::NewGuid().ToString('N')).$Extension"
    $words = @($ShellCommand | ForEach-Object { $_.Replace('{remote}', $remote) })
    try {
        Invoke-NativeCommand -FilePath $Adb -Arguments (@('-s', $Serial, 'shell') + $words) `
            -FailureMessage $FailureMessage | Out-Null
        Invoke-NativeCommand -FilePath $Adb -Arguments @('-s', $Serial, 'pull', $remote, $outputPath) `
            -FailureMessage "ADB failed to pull $remote from $Serial" | Out-Null
        if (-not (Test-Path -LiteralPath $outputPath)) { throw "ADB did not write $outputPath." }
    }
    finally {
        Invoke-NativeCommand -FilePath $Adb -Arguments @('-s', $Serial, 'shell', 'rm', '-f', $remote) | Out-Null
    }
    return $outputPath
}

function Save-DeviceScreenshot {
    param(
        [Parameter(Mandatory)]
        [string]$Adb,
        [Parameter(Mandatory)]
        [string]$Serial,
        [Parameter(Mandatory)]
        [string]$Output
    )

    Save-DeviceFile -Adb $Adb -Serial $Serial -Extension png -Output $Output `
        -ShellCommand @('screencap', '-p', '{remote}') `
        -FailureMessage "Screenshot capture failed on $Serial"
}

function Save-DeviceLayout {
    <#
      Dump the accessibility hierarchy as XML. Compose test tags appear as resource-id when the
      root sets testTagsAsResourceId, which makes this far cheaper for an agent than a screenshot.
    #>
    param(
        [Parameter(Mandatory)]
        [string]$Adb,
        [Parameter(Mandatory)]
        [string]$Serial,
        [Parameter(Mandatory)]
        [string]$Output
    )

    # Only one UiAutomation client can be connected. Android CLI's layout server and Maestro's
    # driver both stay resident after use, and uiautomator is then killed (exit 137).
    Save-DeviceFile -Adb $Adb -Serial $Serial -Extension xml -Output $Output `
        -ShellCommand @('uiautomator', 'dump', '{remote}') `
        -FailureMessage ("UI hierarchy dump failed on $Serial. If Android CLI or Maestro used this " +
            "device, their UiAutomation client blocks uiautomator; use their layout command instead or " +
            "run: adb -s $Serial shell am force-stop com.android.cli.interact.instrumentation")
}

function Get-AuthorizedDeviceSerial {
    param(
        [Parameter(Mandatory)]
        [string]$Adb,
        [string]$RequestedSerial
    )

    $devices = @(Get-AdbDeviceRecord -Adb $Adb)

    if ($RequestedSerial) {
        $match = @($devices | Where-Object { $_.Serial -eq $RequestedSerial })
        if ($match.Count -ne 1 -or $match[0].State -ne 'device') {
            throw "Device '$RequestedSerial' is not connected and authorized."
        }
        return $RequestedSerial
    }

    $authorized = @($devices | Where-Object { $_.State -eq 'device' })
    if ($authorized.Count -eq 0) {
        $states = ($devices | ForEach-Object { "$($_.Serial):$($_.State)" }) -join ', '
        throw "No authorized Android device found. Connected states: $states"
    }
    if ($authorized.Count -gt 1) {
        $serials = ($authorized | ForEach-Object Serial) -join ', '
        throw "Multiple devices found ($serials). Pass -Serial explicitly."
    }
    return $authorized[0].Serial
}

function Get-AdbDeviceRecord {
    param(
        [Parameter(Mandatory)]
        [string]$Adb
    )

    $rows = & $Adb devices | Select-Object -Skip 1 | Where-Object { $_.Trim() }
    if ($LASTEXITCODE -ne 0) {
        throw 'ADB failed to list devices.'
    }
    foreach ($row in $rows) {
        $columns = $row -split '\s+'
        if ($columns.Count -ge 2) {
            [pscustomobject]@{ Serial = $columns[0]; State = $columns[1] }
        }
    }
}

function Wait-AuthorizedDeviceSerial {
    param(
        [Parameter(Mandatory)]
        [string]$Adb,
        [string]$RequestedSerial,
        [ValidateRange(1, 600)]
        [int]$TimeoutSeconds = 60
    )

    $deadline = [DateTimeOffset]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        $devices = @(Get-AdbDeviceRecord -Adb $Adb)
        if ($RequestedSerial) {
            $requested = @($devices | Where-Object { $_.Serial -eq $RequestedSerial })
            if ($requested.Count -eq 1 -and $requested[0].State -eq 'device') {
                return $RequestedSerial
            }
        }
        else {
            $authorized = @($devices | Where-Object { $_.State -eq 'device' })
            if ($authorized.Count -eq 1) { return $authorized[0].Serial }
            if ($authorized.Count -gt 1) {
                $serials = ($authorized | ForEach-Object Serial) -join ', '
                throw "Multiple devices found ($serials). Pass -Serial explicitly."
            }
        }
        Start-Sleep -Milliseconds 500
    } while ([DateTimeOffset]::UtcNow -lt $deadline)

    $target = if ($RequestedSerial) { " '$RequestedSerial'" } else { '' }
    throw "Timed out waiting for authorized Android device$target."
}
