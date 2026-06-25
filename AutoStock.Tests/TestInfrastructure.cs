using AutoStock.Repositories;
using AutoStock.Repositories.Entities;
using AutoStock.Repositories.Enums;
using AutoStock.Services.Dtos.AuditLogs;
using AutoStock.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AutoStock.Tests;

internal sealed class TestDatabase : IAsyncDisposable
{
    private TestDatabase(AppDbContext context)
    {
        Context = context;
    }

    public AppDbContext Context { get; }

    public static async Task<TestDatabase> CreateAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"AutoStockTests-{Guid.NewGuid():N}")
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .EnableSensitiveDataLogging(false)
            .Options;

        var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        return new TestDatabase(context);
    }

    public async Task<SeededRecord> SeedServiceRecordAsync(
        int workshopId = 1,
        int userId = 10,
        int serviceRecordId = 100)
    {
        var workshop = new Workshop
        {
            Id = workshopId,
            Name = $"Workshop {workshopId}",
            CreatedAt = DateTime.UtcNow,
            SubscriptionStartDate = DateTime.UtcNow
        };
        var user = new AppUser
        {
            Id = userId,
            UserName = $"user{userId}",
            NormalizedUserName = $"USER{userId}",
            FullName = $"User {userId}",
            CreatedAt = DateTime.UtcNow
        };
        var customer = new Customer
        {
            Id = serviceRecordId,
            WorkshopId = workshopId,
            Type = CustomerType.Individual,
            FullName = "Test Customer",
            PhoneNumber = "05000000000",
            CreatedAt = DateTime.UtcNow
        };
        var vehicle = new Vehicle
        {
            Id = serviceRecordId,
            WorkshopId = workshopId,
            CustomerId = customer.Id,
            Plate = $"34TEST{serviceRecordId}",
            CreatedAt = DateTime.UtcNow
        };
        var record = new ServiceRecord
        {
            Id = serviceRecordId,
            WorkshopId = workshopId,
            CustomerId = customer.Id,
            VehicleId = vehicle.Id,
            RecordNumber = $"SR-{serviceRecordId}",
            CustomerNameSnapshot = customer.FullName!,
            CustomerPhoneSnapshot = customer.PhoneNumber,
            VehiclePlateSnapshot = vehicle.Plate,
            CreatedAt = DateTime.UtcNow
        };

        Context.AddRange(workshop, user, customer, vehicle, record);
        await Context.SaveChangesAsync();

        return new SeededRecord(workshop, user, customer, vehicle, record);
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
    }
}

internal sealed record SeededRecord(
    Workshop Workshop,
    AppUser User,
    Customer Customer,
    Vehicle Vehicle,
    ServiceRecord ServiceRecord);

internal sealed class MutableDateTimeProvider : IDateTimeProvider
{
    public MutableDateTimeProvider(DateTime utcNow)
    {
        UtcNow = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
    }

    public DateTime Now => UtcNow.ToLocalTime();
    public DateTime Today => Now.Date;
    public DateTime UtcNow { get; private set; }
    public DateTime TodayStartUtc => Today.ToUniversalTime();
    public DateTime TomorrowStartUtc => Today.AddDays(1).ToUniversalTime();

    public void Advance(TimeSpan duration)
    {
        UtcNow = UtcNow.Add(duration);
    }
}

internal sealed class RecordingAuditLogService : IAuditLogService
{
    public List<AuditLogCreateDto> Entries { get; } = [];

    public Task AddAsync(AuditLogCreateDto request, CancellationToken cancellationToken = default)
    {
        Entries.Add(request);
        return Task.CompletedTask;
    }

    public Task WriteAsync(AuditLogCreateDto request, CancellationToken cancellationToken = default)
    {
        Entries.Add(request);
        return Task.CompletedTask;
    }
}
