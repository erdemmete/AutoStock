using AutoStock.Services.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoStock.Tests;

public class EntityEditLockServiceTests
{
    [Fact]
    public async Task FirstUserCanAcquireAndHeartbeatWithSameToken()
    {
        await using var database = await TestDatabase.CreateAsync();
        var seeded = await database.SeedServiceRecordAsync();
        var time = new MutableDateTimeProvider(new DateTime(2026, 6, 24, 12, 0, 0, DateTimeKind.Utc));
        var service = CreateService(database, time);

        var acquired = await service.AcquireAsync(
            EntityEditLockService.ServiceRecordEntityType,
            seeded.ServiceRecord.Id,
            null,
            seeded.Workshop.Id,
            seeded.User.Id);

        Assert.True(acquired.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(acquired.Data?.LockToken));

        time.Advance(TimeSpan.FromSeconds(40));
        var heartbeat = await service.HeartbeatAsync(
            EntityEditLockService.ServiceRecordEntityType,
            seeded.ServiceRecord.Id,
            acquired.Data!.LockToken,
            seeded.Workshop.Id,
            seeded.User.Id);

        Assert.True(heartbeat.IsSuccess);
    }

    [Fact]
    public async Task ExpiredTokenIsRejectedAndStaleLockCanBeTakenOver()
    {
        await using var database = await TestDatabase.CreateAsync();
        var seeded = await database.SeedServiceRecordAsync();
        var secondUser = new AutoStock.Repositories.Entities.AppUser
        {
            Id = 11,
            UserName = "second",
            NormalizedUserName = "SECOND",
            FullName = "Second User",
            CreatedAt = DateTime.UtcNow
        };
        database.Context.Users.Add(secondUser);
        await database.Context.SaveChangesAsync();

        var time = new MutableDateTimeProvider(new DateTime(2026, 6, 24, 12, 0, 0, DateTimeKind.Utc));
        var service = CreateService(database, time);
        var first = await service.AcquireAsync(
            EntityEditLockService.ServiceRecordEntityType,
            seeded.ServiceRecord.Id,
            null,
            seeded.Workshop.Id,
            seeded.User.Id);

        time.Advance(EntityEditLockService.LockDuration.Add(TimeSpan.FromSeconds(1)));

        var expiredHeartbeat = await service.HeartbeatAsync(
            EntityEditLockService.ServiceRecordEntityType,
            seeded.ServiceRecord.Id,
            first.Data!.LockToken,
            seeded.Workshop.Id,
            seeded.User.Id);
        var takeover = await service.AcquireAsync(
            EntityEditLockService.ServiceRecordEntityType,
            seeded.ServiceRecord.Id,
            null,
            seeded.Workshop.Id,
            secondUser.Id);

        Assert.True(expiredHeartbeat.IsFailure);
        Assert.Equal(EntityEditLockService.ExpiredLockCode, expiredHeartbeat.ErrorCode);
        Assert.True(takeover.IsSuccess);
        Assert.NotEqual(first.Data.LockToken, takeover.Data!.LockToken);
    }

    [Fact]
    public async Task AnotherUserCannotAcquireAnActiveLock()
    {
        await using var database = await TestDatabase.CreateAsync();
        var seeded = await database.SeedServiceRecordAsync();
        var secondUser = new AutoStock.Repositories.Entities.AppUser
        {
            Id = 11,
            UserName = "second",
            NormalizedUserName = "SECOND",
            FullName = "Second User",
            CreatedAt = DateTime.UtcNow
        };
        database.Context.Users.Add(secondUser);
        await database.Context.SaveChangesAsync();

        var time = new MutableDateTimeProvider(DateTime.UtcNow);
        var service = CreateService(database, time);
        await service.AcquireAsync(
            EntityEditLockService.ServiceRecordEntityType,
            seeded.ServiceRecord.Id,
            null,
            seeded.Workshop.Id,
            seeded.User.Id);

        var denied = await service.AcquireAsync(
            EntityEditLockService.ServiceRecordEntityType,
            seeded.ServiceRecord.Id,
            null,
            seeded.Workshop.Id,
            secondUser.Id);

        Assert.True(denied.IsFailure);
        Assert.Equal(EntityEditLockService.HeldByAnotherUserCode, denied.ErrorCode);
        Assert.True(denied.Data?.IsLockedByAnotherUser);
    }

    [Fact]
    public async Task AdminForceReleaseInvalidatesOldToken()
    {
        await using var database = await TestDatabase.CreateAsync();
        var seeded = await database.SeedServiceRecordAsync();
        var admin = new AutoStock.Repositories.Entities.AppUser
        {
            Id = 99,
            UserName = "admin",
            NormalizedUserName = "ADMIN",
            FullName = "Admin",
            CreatedAt = DateTime.UtcNow
        };
        database.Context.Users.Add(admin);
        await database.Context.SaveChangesAsync();

        var time = new MutableDateTimeProvider(DateTime.UtcNow);
        var audit = new RecordingAuditLogService();
        var service = CreateService(database, time, audit);
        var acquired = await service.AcquireAsync(
            EntityEditLockService.ServiceRecordEntityType,
            seeded.ServiceRecord.Id,
            null,
            seeded.Workshop.Id,
            seeded.User.Id);

        var released = await service.ForceReleaseForAdminAsync(
            EntityEditLockService.ServiceRecordEntityType,
            seeded.ServiceRecord.Id,
            seeded.Workshop.Id,
            admin.Id);
        var oldHeartbeat = await service.HeartbeatAsync(
            EntityEditLockService.ServiceRecordEntityType,
            seeded.ServiceRecord.Id,
            acquired.Data!.LockToken,
            seeded.Workshop.Id,
            seeded.User.Id);

        Assert.True(released.IsSuccess);
        Assert.True(oldHeartbeat.IsFailure);
        Assert.Equal(EntityEditLockService.InvalidLockCode, oldHeartbeat.ErrorCode);
        Assert.Single(audit.Entries);
        Assert.Equal(admin.Id, audit.Entries[0].UserId);
    }

    [Fact]
    public async Task AdminCannotReleaseEntityFromAnotherWorkshop()
    {
        await using var database = await TestDatabase.CreateAsync();
        var seeded = await database.SeedServiceRecordAsync();
        database.Context.Workshops.Add(new AutoStock.Repositories.Entities.Workshop
        {
            Id = 2,
            Name = "Other Workshop",
            CreatedAt = DateTime.UtcNow,
            SubscriptionStartDate = DateTime.UtcNow
        });
        await database.Context.SaveChangesAsync();

        var service = CreateService(database, new MutableDateTimeProvider(DateTime.UtcNow));
        var result = await service.ForceReleaseForAdminAsync(
            EntityEditLockService.ServiceRecordEntityType,
            seeded.ServiceRecord.Id,
            2,
            99);

        Assert.True(result.IsFailure);
        Assert.Equal(EntityEditLockService.EntityNotFoundCode, result.ErrorCode);
    }

    private static EntityEditLockService CreateService(
        TestDatabase database,
        MutableDateTimeProvider time,
        RecordingAuditLogService? audit = null)
    {
        return new EntityEditLockService(
            database.Context,
            time,
            audit ?? new RecordingAuditLogService(),
            NullLogger<EntityEditLockService>.Instance);
    }
}
