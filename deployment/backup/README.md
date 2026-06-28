# Sente360 Production Backup and Restore Runbook

This folder contains a dry-run friendly disaster recovery backup plan for the
Sente360 / AutoStock production server. The scripts are intentionally
configuration-driven and do not contain production secrets.

## Current application storage map

The current code stores persistent user data in these places:

- SQL Server database: `AutoStockProdDb`.
- Official invoice PDFs uploaded by accountants: configured by
  `Storage:OfficialInvoiceUploadsPath` in the API. The current fallback is
  `AppContext.BaseDirectory\Uploads`, with files under `official-invoices/...`.
- Service record photos: API current directory under
  `App_Data\service-record-images\{WorkshopId}\{ServiceRecordId}`.
- API/WEB production configuration: `appsettings*.json`, `web.config`, and IIS
  site/AppPool configuration.

Do not treat these as irreplaceable production data:

- `bin`, `obj`, publish binaries, temporary caches.
- `Logs` and `*.log` files. Keep logs through observability/retention, not DR.
- Vehicle catalog JSON/source data if it is already in Git/release artifacts.

## Files

- `backup-settings.example.ps1`: copy to `backup-settings.ps1` on the server and
  adjust paths.
- `Backup-Sente360.ps1`: creates SQL `.bak`, copies configured persistent files,
  optionally sends the run to restic, then applies guarded local retention.
- `Restore-Test-Sente360.ps1`: verifies/restores a backup into a test database
  only. It refuses production-looking database names.

## First-time setup

1. Install SQL Server command line tools so `sqlcmd` is available.
2. Install restic if remote encrypted backup is enabled.
3. Create a backup root, for example:

   ```powershell
   New-Item -ItemType Directory -Force -Path D:\Sente360Backup
   ```

4. Copy settings:

   ```powershell
   Copy-Item .\backup-settings.example.ps1 .\backup-settings.ps1
   ```

5. Edit `backup-settings.ps1`:

   - `Sql.Instance`
   - `Sql.DatabaseName`
   - `Paths.BackupRoot`
   - `FileSources`
   - restore test path

6. Ensure the scheduled task identity has:

   - SQL permission to run `BACKUP DATABASE` and `RESTORE VERIFYONLY`.
   - Read access to API/WEB deployment folders.
   - Read/write access to `BackupRoot`.

## Restic configuration

Keep restic secrets out of the repository and out of appsettings files.

Required environment variables:

```powershell
setx RESTIC_REPOSITORY "s3:https://s3.example.com/sente360-prod-backup"
setx RESTIC_PASSWORD_FILE "D:\Sente360Secrets\restic-password.txt"
```

For S3-compatible storage, configure the provider's environment variables, for
example:

```powershell
setx AWS_ACCESS_KEY_ID "..."
setx AWS_SECRET_ACCESS_KEY "..."
```

Initialize the repository once:

```powershell
restic init
```

Default retention:

- Local run folders: 3 days.
- Remote restic snapshots: 14 daily, 8 weekly, 12 monthly.

If remote backup fails, the local run folder is left in place for inspection.

## Dry run

Run this before creating a scheduled task:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File D:\Sente360BackupScripts\Backup-Sente360.ps1 -SettingsPath D:\Sente360BackupScripts\backup-settings.ps1 -DryRun
```

For a local smoke test without SQL/restic:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Backup-Sente360.ps1 -SettingsPath .\backup-settings.ps1 -DryRun -SkipSqlBackup -SkipRestic
```

## Scheduled task

Use Windows Task Scheduler, not SQL Server Agent.

Recommended settings:

- Trigger 1: daily at `03:00`.
- Trigger 2: daily at `15:00`.
- Run whether user is logged on or not.
- Run with highest privileges.
- Do not start a new instance if the task is already running.
- Retry every 15 minutes up to 3 times.
- Run as soon as possible after a missed scheduled start.

Example action:

```text
Program: powershell.exe
Arguments: -NoProfile -ExecutionPolicy Bypass -File "D:\Sente360BackupScripts\Backup-Sente360.ps1" -SettingsPath "D:\Sente360BackupScripts\backup-settings.ps1"
Start in: D:\Sente360BackupScripts
```

## Restore test

Never restore directly over production.

Verify only:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Restore-Test-Sente360.ps1 -SettingsPath .\backup-settings.ps1 -VerifyOnly
```

Restore latest backup to a test database:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Restore-Test-Sente360.ps1 -SettingsPath .\backup-settings.ps1 -TestDatabaseName AutoStockRestoreTestDb
```

If the test DB already exists and you intentionally want to overwrite it:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Restore-Test-Sente360.ps1 -SettingsPath .\backup-settings.ps1 -TestDatabaseName AutoStockRestoreTestDb -AllowReplace
```

The restore script uses:

- `RESTORE VERIFYONLY ... WITH CHECKSUM`
- `RESTORE FILELISTONLY`
- `RESTORE DATABASE ... WITH MOVE`

## DR order

1. Provision Windows/IIS/SQL Server.
2. Restore `AutoStockProdDb` from the latest verified `.bak`.
3. Restore API/WEB deployment configuration.
4. Restore upload folders:
   - API `Uploads`
   - API `App_Data\service-record-images`
5. Re-deploy application binaries from the known release artifact.
6. Reapply IIS AppPool identities and filesystem permissions.
7. Smoke test:
   - Login.
   - Service record detail with photos.
   - Accountant official invoice PDF download/upload.
   - Invoice/account summary detail.
   - QR/public pages.

## IIS and filesystem permissions

The production API AppPool identity needs modify access to:

- API `Uploads` path used by `Storage:OfficialInvoiceUploadsPath`.
- API `App_Data\service-record-images`.

The backup task identity needs read access to those paths and write access to the
backup root.

## Failure notification

`Backup-Sente360.ps1` has an optional notification block. Configure it only with
environment variables. Do not place SMTP passwords in this repository or the
settings file.

## Test checklist

Run these before relying on the automation:

- PowerShell syntax check for both scripts.
- Dry-run backup.
- Dry-run with invalid `BackupRoot`; it must fail path validation.
- Start two backup runs; the second must fail because of the named mutex.
- Simulate SQL failure with `-SimulateSqlFailure`; no remote retention should run.
- Simulate restic failure with `-SimulateResticFailure`; local run folder remains.
- Confirm local retention only deletes child folders under `BackupRoot\runs`.
- Restore script refuses `AutoStockProdDb` and production-looking names.
- Restore test DB from a verified backup.

## Secrets and manual rotation

The repository currently contains development/sample configuration files that may
include secrets. Do not copy those values into production scripts. Before final
production readiness, rotate and move these to environment variables or server
secret storage:

- SQL passwords / connection strings.
- SMTP passwords.
- JWT signing key.
- VAPID private key.
- Restic repository password and cloud access keys.

Secret rotation can invalidate sessions or web push subscriptions. Plan it as a
separate operational change.
