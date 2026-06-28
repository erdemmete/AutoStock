[CmdletBinding()]
param(
    [string]$SettingsPath = (Join-Path $PSScriptRoot "backup-settings.ps1"),
    [string]$BackupFile,
    [string]$TestDatabaseName,
    [string]$SqlDataPath,
    [switch]$AllowReplace,
    [switch]$VerifyOnly,
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-RestoreLog {
    param([string]$Message)
    Write-Host ("{0:u} {1}" -f (Get-Date), $Message)
}

function Get-FullPathSafe {
    param([string]$Path)
    return [System.IO.Path]::GetFullPath($Path)
}

function Join-SafePath {
    param([string]$Left, [string]$Right)
    return [System.IO.Path]::Combine($Left, $Right)
}

function Invoke-SqlCommandForRestore {
    param([hashtable]$Sql, [string]$Query, [string]$DisplayName, [switch]$CaptureOutput)

    $sqlcmd = [string]$Sql.SqlCmdExe
    if (-not (Get-Command $sqlcmd -ErrorAction SilentlyContinue)) {
        if ($DryRun) {
            Write-RestoreLog "DRY RUN: sqlcmd was not found. Real restore verification requires SQL Server command line tools."
            return @()
        }
        throw "sqlcmd was not found. Install SQL Server command line tools or set Sql.SqlCmdExe."
    }

    $args = @("-S", [string]$Sql.Instance, "-b", "-Q", $Query)
    if ($Sql.UseWindowsAuthentication) {
        $args += "-E"
    }
    else {
        $passwordVariable = [string]$Sql.PasswordEnvironmentVariable
        $password = [Environment]::GetEnvironmentVariable($passwordVariable)
        if ([string]::IsNullOrWhiteSpace($password)) {
            throw "SQL password environment variable is missing: $passwordVariable"
        }
        $args += @("-U", [string]$Sql.Username, "-P", $password)
    }

    Write-RestoreLog "$DisplayName starting."
    if ($DryRun) {
        Write-RestoreLog "DRY RUN: $sqlcmd $($args -join ' ')"
        return @()
    }

    if ($CaptureOutput) {
        $output = & $sqlcmd @args
        if ($LASTEXITCODE -ne 0) { throw "$DisplayName failed with exit code $LASTEXITCODE." }
        return $output
    }

    & $sqlcmd @args
    if ($LASTEXITCODE -ne 0) { throw "$DisplayName failed with exit code $LASTEXITCODE." }
    return @()
}

function Escape-SqlString {
    param([string]$Value)
    return $Value.Replace("'", "''")
}

if (-not (Test-Path -LiteralPath $SettingsPath)) {
    throw "Settings file not found: $SettingsPath"
}

. $SettingsPath
if (-not $BackupSettings) {
    throw "Settings file did not define `$BackupSettings."
}

$prodDb = [string]$BackupSettings.Sql.DatabaseName
if ([string]::IsNullOrWhiteSpace($TestDatabaseName)) {
    $TestDatabaseName = [string]$BackupSettings.Sql.RestoreTestDatabaseName
}
if ([string]::IsNullOrWhiteSpace($TestDatabaseName)) {
    $TestDatabaseName = "AutoStockRestoreTestDb"
}

if ($TestDatabaseName.Equals($prodDb, [System.StringComparison]::OrdinalIgnoreCase) -or
    $TestDatabaseName -match '(?i)prod') {
    throw "Refusing to restore to a production-looking database name: $TestDatabaseName"
}

if ([string]::IsNullOrWhiteSpace($BackupFile)) {
    $backupRoot = Get-FullPathSafe ([string]$BackupSettings.Paths.BackupRoot)
    $runsRoot = Join-SafePath $backupRoot ([string]$BackupSettings.Paths.RunsDirectoryName)
    $latest = Get-ChildItem -LiteralPath $runsRoot -Filter "*.bak" -File -Recurse -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
    if (-not $latest) { throw "No .bak file found under $runsRoot" }
    $BackupFile = $latest.FullName
}

$BackupFile = Get-FullPathSafe $BackupFile
if (-not (Test-Path -LiteralPath $BackupFile) -and -not $DryRun) {
    throw "Backup file not found: $BackupFile"
}

if ([string]::IsNullOrWhiteSpace($SqlDataPath)) {
    $SqlDataPath = [string]$BackupSettings.Sql.RestoreTestDataPath
}
if ([string]::IsNullOrWhiteSpace($SqlDataPath)) {
    $SqlDataPath = Join-SafePath ([string]$BackupSettings.Paths.BackupRoot) "restore-test-data"
}

$SqlDataPath = Get-FullPathSafe $SqlDataPath
if (-not $DryRun) {
    New-Item -ItemType Directory -Force -Path $SqlDataPath | Out-Null
}

$escapedBak = Escape-SqlString $BackupFile
Invoke-SqlCommandForRestore -Sql $BackupSettings.Sql -Query "RESTORE VERIFYONLY FROM DISK = N'$escapedBak' WITH CHECKSUM;" -DisplayName "RESTORE VERIFYONLY" | Out-Null

if ($DryRun) {
    Write-RestoreLog "DRY RUN: would run RESTORE FILELISTONLY and restore into test database $TestDatabaseName."
    return
}

if ($VerifyOnly) {
    Write-RestoreLog "Verify-only requested. Restore was not executed."
    return
}

$fileListQuery = @"
DECLARE @files TABLE (
    LogicalName nvarchar(128),
    PhysicalName nvarchar(260),
    [Type] char(1),
    FileGroupName nvarchar(128) NULL,
    Size numeric(20,0),
    MaxSize numeric(20,0),
    FileId bigint,
    CreateLSN numeric(25,0) NULL,
    DropLSN numeric(25,0) NULL,
    UniqueId uniqueidentifier,
    ReadOnlyLSN numeric(25,0) NULL,
    ReadWriteLSN numeric(25,0) NULL,
    BackupSizeInBytes bigint,
    SourceBlockSize int,
    FileGroupId int,
    LogGroupGUID uniqueidentifier NULL,
    DifferentialBaseLSN numeric(25,0) NULL,
    DifferentialBaseGUID uniqueidentifier NULL,
    IsReadOnly bit,
    IsPresent bit,
    TDEThumbprint varbinary(32) NULL,
    SnapshotUrl nvarchar(360) NULL
);
INSERT INTO @files EXEC('RESTORE FILELISTONLY FROM DISK = N''$escapedBak''');
SELECT LogicalName + N'|' + [Type] FROM @files WHERE [Type] IN ('D','L');
"@

$fileListOutput = Invoke-SqlCommandForRestore -Sql $BackupSettings.Sql -Query $fileListQuery -DisplayName "RESTORE FILELISTONLY" -CaptureOutput
$logicalData = $null
$logicalLog = $null
foreach ($line in $fileListOutput) {
    $trimmed = [string]$line
    if ($trimmed -notmatch '\|') { continue }
    $parts = $trimmed.Split('|')
    if ($parts.Count -lt 2) { continue }
    if ($parts[1].Trim() -eq "D" -and -not $logicalData) { $logicalData = $parts[0].Trim() }
    if ($parts[1].Trim() -eq "L" -and -not $logicalLog) { $logicalLog = $parts[0].Trim() }
}

if (-not $logicalData -or -not $logicalLog) {
    throw "Could not determine logical MDF/LDF names from backup."
}

$escapedDb = $TestDatabaseName.Replace("]", "]]")
$escapedLogicalData = Escape-SqlString $logicalData
$escapedLogicalLog = Escape-SqlString $logicalLog
$mdf = Escape-SqlString (Join-SafePath $SqlDataPath ($TestDatabaseName + ".mdf"))
$ldf = Escape-SqlString (Join-SafePath $SqlDataPath ($TestDatabaseName + "_log.ldf"))

$existsQuery = "IF DB_ID(N'$(Escape-SqlString $TestDatabaseName)') IS NULL SELECT '0' ELSE SELECT '1';"
$existsOutput = Invoke-SqlCommandForRestore -Sql $BackupSettings.Sql -Query $existsQuery -DisplayName "Check test database" -CaptureOutput
$exists = ($existsOutput | Where-Object { $_ -match '^[01]$' } | Select-Object -First 1)
if ($exists -eq "1" -and -not $AllowReplace) {
    throw "Test database already exists. Re-run with -AllowReplace if this is intentional: $TestDatabaseName"
}

$replaceClause = ""
if ($AllowReplace) { $replaceClause = ", REPLACE" }

$restoreQuery = @"
RESTORE DATABASE [$escapedDb]
FROM DISK = N'$escapedBak'
WITH MOVE N'$escapedLogicalData' TO N'$mdf',
     MOVE N'$escapedLogicalLog' TO N'$ldf',
     CHECKSUM,
     RECOVERY,
     STATS = 10$replaceClause;
"@

Invoke-SqlCommandForRestore -Sql $BackupSettings.Sql -Query $restoreQuery -DisplayName "Restore test database" | Out-Null

$sampleQuery = @"
SELECT 'Workshops' AS [Table], COUNT(*) AS [Count] FROM [$escapedDb].dbo.Workshops
UNION ALL SELECT 'Users', COUNT(*) FROM [$escapedDb].dbo.AspNetUsers
UNION ALL SELECT 'ServiceRecords', COUNT(*) FROM [$escapedDb].dbo.ServiceRecords
UNION ALL SELECT 'Invoices', COUNT(*) FROM [$escapedDb].dbo.Invoices;
"@
Invoke-SqlCommandForRestore -Sql $BackupSettings.Sql -Query $sampleQuery -DisplayName "Sample restore checks" | Out-Null
Write-RestoreLog "Restore test completed for database $TestDatabaseName."
