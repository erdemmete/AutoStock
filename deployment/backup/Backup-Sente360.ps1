[CmdletBinding()]
param(
    [string]$SettingsPath = (Join-Path $PSScriptRoot "backup-settings.ps1"),
    [switch]$DryRun,
    [switch]$SkipSqlBackup,
    [switch]$SkipFileCopy,
    [switch]$SkipRestic,
    [switch]$SimulateSqlFailure,
    [switch]$SimulateResticFailure
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RunStartedAt = Get-Date
$Timestamp = $RunStartedAt.ToString("yyyyMMdd-HHmmss")
$LogFile = $null
$RunDirectory = $null
$Mutex = $null

function Write-BackupLog {
    param(
        [ValidateSet("INFO", "WARN", "ERROR")]
        [string]$Level,
        [string]$Message
    )

    $line = "{0:u} [{1}] {2}" -f (Get-Date), $Level, $Message
    Write-Host $line
    if ($script:LogFile) {
        $logDirectory = Split-Path -Parent $script:LogFile
        if (Test-Path -LiteralPath $logDirectory) {
            Add-Content -LiteralPath $script:LogFile -Value $line
        }
    }
}

function Get-RequiredSetting {
    param([hashtable]$Table, [string]$Key)
    if (-not $Table.ContainsKey($Key) -or $null -eq $Table[$Key] -or [string]::IsNullOrWhiteSpace([string]$Table[$Key])) {
        throw "Missing backup setting: $Key"
    }
    return $Table[$Key]
}

function Get-FullPathSafe {
    param([string]$Path)
    return [System.IO.Path]::GetFullPath($Path)
}

function Join-SafePath {
    param([string]$Left, [string]$Right)
    return [System.IO.Path]::Combine($Left, $Right)
}

function Assert-SafeBackupRoot {
    param([string]$Path)

    $full = Get-FullPathSafe $Path
    $root = [System.IO.Path]::GetPathRoot($full)
    if ([string]::IsNullOrWhiteSpace($full) -or $full -eq $root -or $full.Length -lt 8) {
        throw "BackupRoot is not safe: $full"
    }
    return $full.TrimEnd('\')
}

function Assert-ChildPath {
    param([string]$Parent, [string]$Child)

    $parentFull = (Get-FullPathSafe $Parent).TrimEnd('\') + '\'
    $childFull = Get-FullPathSafe $Child
    if (-not $childFull.StartsWith($parentFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Unsafe path outside backup root: $childFull"
    }
    return $childFull
}

function ConvertTo-SafeName {
    param([string]$Value)
    $safe = $Value -replace '[^a-zA-Z0-9._-]+', '-'
    return $safe.Trim('-')
}

function Invoke-ExternalCommand {
    param(
        [string]$FilePath,
        [string[]]$Arguments,
        [string]$DisplayName
    )

    Write-BackupLog INFO "$DisplayName starting."
    if ($DryRun) {
        Write-BackupLog INFO "DRY RUN: $FilePath $($Arguments -join ' ')"
        return
    }

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$DisplayName failed with exit code $LASTEXITCODE."
    }
}

function Get-SqlCmdArguments {
    param([hashtable]$Sql, [string]$Query)

    $args = @("-S", [string]$Sql.Instance, "-b", "-Q", $Query)
    if ($Sql.UseWindowsAuthentication) {
        $args += "-E"
    }
    else {
        if ([string]::IsNullOrWhiteSpace([string]$Sql.Username)) {
            throw "Sql.Username is required when UseWindowsAuthentication is false."
        }
        $passwordVariable = [string]$Sql.PasswordEnvironmentVariable
        $password = [Environment]::GetEnvironmentVariable($passwordVariable)
        if ([string]::IsNullOrWhiteSpace($password)) {
            throw "SQL password environment variable is missing: $passwordVariable"
        }
        $args += @("-U", [string]$Sql.Username, "-P", $password)
    }

    if ($Sql.ContainsKey("BackupTimeoutSeconds") -and $Sql.BackupTimeoutSeconds) {
        $args += @("-t", [string]$Sql.BackupTimeoutSeconds)
    }

    return $args
}

function Invoke-SqlQuery {
    param([hashtable]$Sql, [string]$Query, [string]$DisplayName)

    $sqlcmd = [string]$Sql.SqlCmdExe
    if (-not (Get-Command $sqlcmd -ErrorAction SilentlyContinue)) {
        if ($DryRun) {
            Write-BackupLog WARN "DRY RUN: sqlcmd was not found. Real SQL backup requires SQL Server command line tools."
            return
        }
        throw "sqlcmd was not found. Install SQL Server command line tools or set Sql.SqlCmdExe."
    }

    $args = Get-SqlCmdArguments -Sql $Sql -Query $Query
    Invoke-ExternalCommand -FilePath $sqlcmd -Arguments $args -DisplayName $DisplayName
}

function New-SqlBackup {
    param([hashtable]$Sql, [string]$SqlDirectory)

    if ($SimulateSqlFailure) {
        throw "Simulated SQL backup failure."
    }

    $db = [string](Get-RequiredSetting $Sql "DatabaseName")
    $bak = Join-SafePath $SqlDirectory ("{0}_FULL_{1}.bak" -f $db, $script:Timestamp)
    Assert-ChildPath -Parent $script:RunDirectory -Child $bak | Out-Null

    if (-not $DryRun) {
        New-Item -ItemType Directory -Force -Path $SqlDirectory | Out-Null
        if (Test-Path -LiteralPath $bak) {
            throw "Backup file already exists: $bak"
        }
    }

    $escapedBak = $bak.Replace("'", "''")
    $escapedDb = $db.Replace("]", "]]")
    $backupQuery = @"
BACKUP DATABASE [$escapedDb]
TO DISK = N'$escapedBak'
WITH NOFORMAT, NOINIT, CHECKSUM, COMPRESSION, STATS = 10;
"@
    Invoke-SqlQuery -Sql $Sql -Query $backupQuery -DisplayName "SQL backup"

    $verifyQuery = "RESTORE VERIFYONLY FROM DISK = N'$escapedBak' WITH CHECKSUM;"
    Invoke-SqlQuery -Sql $Sql -Query $verifyQuery -DisplayName "SQL backup verify"

    return $bak
}

function Copy-BackupSource {
    param([hashtable]$Source, [string]$FilesDirectory, [string[]]$ExcludePatterns)

    $name = [string](Get-RequiredSetting $Source "Name")
    $sourcePath = [string](Get-RequiredSetting $Source "Path")
    $required = $true
    if ($Source.ContainsKey("Required")) { $required = [bool]$Source.Required }

    if (-not (Test-Path -LiteralPath $sourcePath)) {
        $message = "Source path does not exist: $name ($sourcePath)"
        if ($DryRun -or -not $required) {
            Write-BackupLog WARN $message
            return @{ Name = $name; Path = $sourcePath; Copied = $false; Missing = $true }
        }
        throw $message
    }

    $safeName = ConvertTo-SafeName $name
    $target = Join-SafePath $FilesDirectory $safeName
    Assert-ChildPath -Parent $script:RunDirectory -Child $target | Out-Null

    Write-BackupLog INFO "Copying source '$name'."
    if ($DryRun) {
        Write-BackupLog INFO "DRY RUN: copy '$sourcePath' to '$target'"
        return @{ Name = $name; Path = $sourcePath; Target = $target; Copied = $false; DryRun = $true }
    }

    New-Item -ItemType Directory -Force -Path $FilesDirectory | Out-Null
    $item = Get-Item -LiteralPath $sourcePath
    if ($item.PSIsContainer) {
        New-Item -ItemType Directory -Force -Path $target | Out-Null
        Get-ChildItem -LiteralPath $sourcePath -Force |
            Where-Object { $ExcludePatterns -notcontains $_.Name } |
            ForEach-Object {
                Copy-Item -LiteralPath $_.FullName -Destination $target -Recurse -Force -Exclude $ExcludePatterns
            }
    }
    else {
        New-Item -ItemType Directory -Force -Path $target | Out-Null
        Copy-Item -LiteralPath $sourcePath -Destination (Join-SafePath $target $item.Name) -Force
    }

    $fileCount = (Get-ChildItem -LiteralPath $target -File -Recurse -ErrorAction SilentlyContinue | Measure-Object).Count
    return @{ Name = $name; Path = $sourcePath; Target = $target; Copied = $true; FileCount = $fileCount }
}

function Invoke-ResticBackup {
    param([hashtable]$Restic, [string]$BackupTarget)

    if ($SimulateResticFailure) {
        throw "Simulated restic failure."
    }

    if (-not [bool]$Restic.Enabled -or $SkipRestic) {
        Write-BackupLog INFO "Restic step skipped."
        return
    }

    $resticExe = [string]$Restic.Exe
    if (-not (Get-Command $resticExe -ErrorAction SilentlyContinue)) {
        if ($DryRun) {
            Write-BackupLog WARN "DRY RUN: restic was not found. Real remote backup requires restic."
            return
        }
        throw "restic was not found. Install restic or set Restic.Exe."
    }

    foreach ($varName in $Restic.RequireEnvironmentVariables) {
        if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable([string]$varName))) {
            if ($DryRun) {
                Write-BackupLog WARN "DRY RUN: required restic environment variable is missing: $varName"
                continue
            }
            throw "Required restic environment variable is missing: $varName"
        }
    }

    $tags = @()
    foreach ($tag in $Restic.Tags) {
        $tags += @("--tag", [string]$tag)
    }

    $backupArgs = @("backup", $BackupTarget) + $tags
    if ($Restic.ContainsKey("UseFsSnapshot") -and [bool]$Restic.UseFsSnapshot) {
        $backupArgs += "--use-fs-snapshot"
    }
    Invoke-ExternalCommand -FilePath $resticExe -Arguments $backupArgs -DisplayName "Restic backup"

    $forgetArgs = @(
        "forget",
        "--keep-daily", [string]$Restic.KeepDaily,
        "--keep-weekly", [string]$Restic.KeepWeekly,
        "--keep-monthly", [string]$Restic.KeepMonthly,
        "--prune"
    ) + $tags
    Invoke-ExternalCommand -FilePath $resticExe -Arguments $forgetArgs -DisplayName "Restic retention"
}

function Invoke-LocalRetention {
    param([string]$RunsRoot, [int]$Days)

    if ($Days -le 0) {
        Write-BackupLog WARN "Local retention skipped because LocalRetentionDays is not positive."
        return
    }

    $runsRootFull = Assert-ChildPath -Parent $script:BackupRoot -Child $RunsRoot
    if (-not (Test-Path -LiteralPath $runsRootFull)) {
        return
    }

    $cutoff = (Get-Date).AddDays(-1 * $Days)
    $oldRuns = Get-ChildItem -LiteralPath $runsRootFull -Directory |
        Where-Object { $_.LastWriteTime -lt $cutoff }

    foreach ($run in $oldRuns) {
        Assert-ChildPath -Parent $runsRootFull -Child $run.FullName | Out-Null
        if ($DryRun) {
            Write-BackupLog INFO "DRY RUN: remove old local backup $($run.FullName)"
        }
        else {
            Write-BackupLog INFO "Removing old local backup $($run.FullName)"
            Remove-Item -LiteralPath $run.FullName -Recurse -Force
        }
    }
}

function Send-FailureNotification {
    param([hashtable]$Notification, [string]$Subject, [string]$Body)

    if (-not $Notification -or -not [bool]$Notification.Enabled) { return }

    try {
        $hostName = [Environment]::GetEnvironmentVariable([string]$Notification.SmtpHostEnvironmentVariable)
        $portRaw = [Environment]::GetEnvironmentVariable([string]$Notification.SmtpPortEnvironmentVariable)
        $user = [Environment]::GetEnvironmentVariable([string]$Notification.SmtpUserEnvironmentVariable)
        $password = [Environment]::GetEnvironmentVariable([string]$Notification.SmtpPasswordEnvironmentVariable)
        $from = [Environment]::GetEnvironmentVariable([string]$Notification.FromEnvironmentVariable)
        $to = [Environment]::GetEnvironmentVariable([string]$Notification.ToEnvironmentVariable)
        if ([string]::IsNullOrWhiteSpace($hostName) -or [string]::IsNullOrWhiteSpace($from) -or [string]::IsNullOrWhiteSpace($to)) {
            Write-BackupLog WARN "Failure notification skipped because SMTP environment variables are incomplete."
            return
        }

        $port = 587
        if (-not [string]::IsNullOrWhiteSpace($portRaw)) { $port = [int]$portRaw }

        $message = [System.Net.Mail.MailMessage]::new($from, $to, $Subject, $Body)
        $client = [System.Net.Mail.SmtpClient]::new($hostName, $port)
        $client.EnableSsl = [bool]$Notification.EnableSsl
        if (-not [string]::IsNullOrWhiteSpace($user)) {
            $client.Credentials = [System.Net.NetworkCredential]::new($user, $password)
        }
        if (-not $DryRun) {
            $client.Send($message)
        }
        Write-BackupLog INFO "Failure notification processed."
    }
    catch {
        Write-BackupLog WARN "Failure notification failed. $($_.Exception.Message)"
    }
}

if (-not (Test-Path -LiteralPath $SettingsPath)) {
    throw "Settings file not found. Copy backup-settings.example.ps1 to backup-settings.ps1 and edit it: $SettingsPath"
}

. $SettingsPath
if (-not $BackupSettings) {
    throw "Settings file did not define `$BackupSettings."
}

$script:BackupRoot = Assert-SafeBackupRoot ([string]$BackupSettings.Paths.BackupRoot)
$runsRoot = Join-SafePath $script:BackupRoot ([string]$BackupSettings.Paths.RunsDirectoryName)
$logsRoot = Join-SafePath $script:BackupRoot ([string]$BackupSettings.Paths.LogsDirectoryName)
$script:RunDirectory = Join-SafePath $runsRoot ("sente360-backup-{0}" -f $Timestamp)
$script:LogFile = Join-SafePath $logsRoot ("backup-{0}.log" -f $Timestamp)

if (-not $DryRun) {
    New-Item -ItemType Directory -Force -Path $logsRoot | Out-Null
}

$createdNew = $false
$Mutex = [System.Threading.Mutex]::new($false, "Global\Sente360ProductionBackup", [ref]$createdNew)
if (-not $Mutex.WaitOne(0)) {
    throw "Another Sente360 backup run is already active."
}

$success = $false
try {
    Write-BackupLog INFO "Sente360 backup started. DryRun=$DryRun"
    Write-BackupLog INFO "Backup root: $script:BackupRoot"

    if (-not $DryRun) {
        New-Item -ItemType Directory -Force -Path $script:RunDirectory | Out-Null
    }
    else {
        Write-BackupLog INFO "DRY RUN: would create run directory $script:RunDirectory"
    }

    $manifest = [ordered]@{
        startedAt = $RunStartedAt.ToString("o")
        environment = $BackupSettings.EnvironmentName
        database = $BackupSettings.Sql.DatabaseName
        runDirectory = $script:RunDirectory
        dryRun = [bool]$DryRun
        sqlBackup = $null
        sources = @()
    }

    if (-not $SkipSqlBackup) {
        $sqlDir = Join-SafePath $script:RunDirectory "sql"
        $manifest.sqlBackup = New-SqlBackup -Sql $BackupSettings.Sql -SqlDirectory $sqlDir
    }
    else {
        Write-BackupLog INFO "SQL backup skipped."
    }

    if (-not $SkipFileCopy) {
        $filesDir = Join-SafePath $script:RunDirectory "files"
        foreach ($source in $BackupSettings.FileSources) {
            $manifest.sources += Copy-BackupSource -Source $source -FilesDirectory $filesDir -ExcludePatterns $BackupSettings.ExcludePatterns
        }
    }
    else {
        Write-BackupLog INFO "File copy skipped."
    }

    $manifest.completedAt = (Get-Date).ToString("o")
    $manifestPath = Join-SafePath $script:RunDirectory "manifest.json"
    if ($DryRun) {
        Write-BackupLog INFO "DRY RUN: would write manifest $manifestPath"
    }
    else {
        $manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
    }

    Invoke-ResticBackup -Restic $BackupSettings.Restic -BackupTarget $script:RunDirectory
    Invoke-LocalRetention -RunsRoot $runsRoot -Days ([int]$BackupSettings.Paths.LocalRetentionDays)

    $success = $true
    Write-BackupLog INFO "Sente360 backup completed successfully."
}
catch {
    $message = $_.Exception.Message
    Write-BackupLog ERROR "Sente360 backup failed. $message"
    Send-FailureNotification -Notification $BackupSettings.Notification -Subject "Sente360 backup failed" -Body $message
    throw
}
finally {
    if ($Mutex) {
        $Mutex.ReleaseMutex() | Out-Null
        $Mutex.Dispose()
    }
    if (-not $success) {
        Write-BackupLog WARN "Local files were not deleted after failure. Inspect the run directory before retrying."
    }
}
