# Copy this file to backup-settings.ps1 and adjust paths on the production VPS.
# Do not commit real secrets. Restic and SMTP secrets must come from environment
# variables or password files outside the repository.

$BackupSettings = @{
    EnvironmentName = "Production"

    Sql = @{
        SqlCmdExe = "sqlcmd"
        Instance = ".\SQLEXPRESS"
        DatabaseName = "AutoStockProdDb"
        UseWindowsAuthentication = $true
        Username = $null
        PasswordEnvironmentVariable = "SENTE360_SQL_PASSWORD"
        BackupTimeoutSeconds = 1800
        RestoreTestDatabaseName = "AutoStockRestoreTestDb"
        RestoreTestDataPath = "D:\Sente360Backup\restore-test-data"
    }

    Paths = @{
        BackupRoot = "D:\Sente360Backup"
        RunsDirectoryName = "runs"
        LogsDirectoryName = "logs"
        LocalRetentionDays = 3
    }

    # Update these paths to match IIS physical paths on the production server.
    # UserData is irreplaceable. Config is sensitive but needed for DR.
    FileSources = @(
        @{
            Name = "API official invoice uploads"
            Path = "D:\Sites\Sente360.Api\Uploads"
            Required = $true
            Kind = "UserData"
        },
        @{
            Name = "API service record images"
            Path = "D:\Sites\Sente360.Api\App_Data\service-record-images"
            Required = $true
            Kind = "UserData"
        },
        @{
            Name = "API production appsettings"
            Path = "D:\Sites\Sente360.Api\appsettings.Production.json"
            Required = $true
            Kind = "Config"
            Sensitive = $true
        },
        @{
            Name = "API web.config"
            Path = "D:\Sites\Sente360.Api\web.config"
            Required = $true
            Kind = "Config"
            Sensitive = $true
        },
        @{
            Name = "WEB production appsettings"
            Path = "D:\Sites\Sente360.Web\appsettings.Production.json"
            Required = $false
            Kind = "Config"
            Sensitive = $true
        },
        @{
            Name = "WEB appsettings"
            Path = "D:\Sites\Sente360.Web\appsettings.json"
            Required = $true
            Kind = "Config"
            Sensitive = $true
        },
        @{
            Name = "WEB web.config"
            Path = "D:\Sites\Sente360.Web\web.config"
            Required = $true
            Kind = "Config"
            Sensitive = $true
        }
    )

    ExcludePatterns = @(
        "bin",
        "obj",
        "Logs",
        "*.log",
        "cache",
        "tmp",
        "Temp"
    )

    Restic = @{
        Enabled = $true
        Exe = "restic"
        RequireEnvironmentVariables = @(
            "RESTIC_REPOSITORY",
            "RESTIC_PASSWORD_FILE"
        )
        Tags = @("sente360", "production")
        UseFsSnapshot = $false
        KeepDaily = 14
        KeepWeekly = 8
        KeepMonthly = 12
    }

    Notification = @{
        Enabled = $false
        SmtpHostEnvironmentVariable = "SENTE360_BACKUP_SMTP_HOST"
        SmtpPortEnvironmentVariable = "SENTE360_BACKUP_SMTP_PORT"
        SmtpUserEnvironmentVariable = "SENTE360_BACKUP_SMTP_USER"
        SmtpPasswordEnvironmentVariable = "SENTE360_BACKUP_SMTP_PASSWORD"
        FromEnvironmentVariable = "SENTE360_BACKUP_MAIL_FROM"
        ToEnvironmentVariable = "SENTE360_BACKUP_MAIL_TO"
        EnableSsl = $true
    }
}
