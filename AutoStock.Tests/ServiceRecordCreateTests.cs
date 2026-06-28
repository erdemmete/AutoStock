using AutoStock.Repositories.Enums;
using AutoStock.Services.Dtos.ServiceRecords;
using AutoStock.Services.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoStock.Tests;

public class ServiceRecordCreateTests
{
    [Fact]
    public async Task RepeatedClientRequestIdReturnsTheExistingServiceRecord()
    {
        await using var database = await TestDatabase.CreateAsync();
        AddWorkshop(database, 1);
        await database.Context.SaveChangesAsync();

        var dateTimeProvider = new MutableDateTimeProvider(new DateTime(2026, 6, 24, 12, 0, 0, DateTimeKind.Utc));
        var service = new ServiceRecordService(
            database.Context,
            null!,
            dateTimeProvider,
            new RecordingAuditLogService(),
            null!,
            NullLogger<ServiceRecordService>.Instance);
        var request = CreateRequest("request-123", "34TEST123");

        var first = await service.CreateAsync(request, 1);
        dateTimeProvider.Advance(TimeSpan.FromMilliseconds(1));
        var second = await service.CreateAsync(request, 1);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Data!.ServiceRecordId, second.Data!.ServiceRecordId);
        Assert.Single(database.Context.ServiceRecords);
    }

    [Fact]
    public async Task DifferentClientRequestIdCreatesANewServiceRecord()
    {
        await using var database = await TestDatabase.CreateAsync();
        AddWorkshop(database, 1);
        await database.Context.SaveChangesAsync();

        var dateTimeProvider = new MutableDateTimeProvider(new DateTime(2026, 6, 24, 12, 0, 0, DateTimeKind.Utc));
        var service = CreateService(database, dateTimeProvider);

        var first = await service.CreateAsync(CreateRequest("request-123", "34TEST123"), 1);
        dateTimeProvider.Advance(TimeSpan.FromMilliseconds(1));
        var second = await service.CreateAsync(CreateRequest("request-456", "34TEST124"), 1);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.NotEqual(first.Data!.ServiceRecordId, second.Data!.ServiceRecordId);
        Assert.Equal(2, database.Context.ServiceRecords.Count());
    }

    [Fact]
    public async Task SameClientRequestIdCanBeUsedByDifferentWorkshopsWithoutDataLeak()
    {
        await using var database = await TestDatabase.CreateAsync();
        AddWorkshop(database, 1);
        AddWorkshop(database, 2);
        await database.Context.SaveChangesAsync();

        var dateTimeProvider = new MutableDateTimeProvider(new DateTime(2026, 6, 24, 12, 0, 0, DateTimeKind.Utc));
        var service = CreateService(database, dateTimeProvider);

        var workshopOne = await service.CreateAsync(CreateRequest("shared-request", "34AAA123"), 1);
        dateTimeProvider.Advance(TimeSpan.FromMilliseconds(1));
        var workshopTwo = await service.CreateAsync(CreateRequest("shared-request", "35BBB123"), 2);
        dateTimeProvider.Advance(TimeSpan.FromMilliseconds(1));
        var workshopTwoRetry = await service.CreateAsync(CreateRequest("shared-request", "35BBB123"), 2);

        Assert.True(workshopOne.IsSuccess);
        Assert.True(workshopTwo.IsSuccess);
        Assert.True(workshopTwoRetry.IsSuccess);
        Assert.NotEqual(workshopOne.Data!.ServiceRecordId, workshopTwo.Data!.ServiceRecordId);
        Assert.Equal(workshopTwo.Data.ServiceRecordId, workshopTwoRetry.Data!.ServiceRecordId);
        Assert.Equal(2, database.Context.ServiceRecords.Count());
        Assert.All(database.Context.ServiceRecords, record =>
        {
            Assert.True(record.WorkshopId is 1 or 2);
        });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyClientRequestIdDoesNotReuseAnotherServiceRecord(string? clientRequestId)
    {
        await using var database = await TestDatabase.CreateAsync();
        AddWorkshop(database, 1);
        await database.Context.SaveChangesAsync();

        var dateTimeProvider = new MutableDateTimeProvider(new DateTime(2026, 6, 24, 12, 0, 0, DateTimeKind.Utc));
        var service = CreateService(database, dateTimeProvider);

        var first = await service.CreateAsync(CreateRequest(clientRequestId, "34NULL123"), 1);
        dateTimeProvider.Advance(TimeSpan.FromMilliseconds(1));
        var second = await service.CreateAsync(CreateRequest(clientRequestId, "34NULL123"), 1);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.NotEqual(first.Data!.ServiceRecordId, second.Data!.ServiceRecordId);
        Assert.Equal(2, database.Context.ServiceRecords.Count());
    }

    [Fact]
    public void ServiceRecordCreateScriptRefreshesRequestIdAfterSuccessfulCreate()
    {
        var script = File.ReadAllText(FindWorkspaceFile(
            "AutoStock.WEB",
            "wwwroot",
            "js",
            "service-record-create.js"));

        Assert.Contains("data.__clientRequestId", script);
        Assert.Contains("clearServiceRecordRequestId();", script);
        Assert.Contains("startNewServiceRecordRequestId();", script);
        Assert.Contains("isServiceRecordCreated = true;", script);
    }

    private static ServiceRecordService CreateService(
        TestDatabase database,
        MutableDateTimeProvider dateTimeProvider)
    {
        return new ServiceRecordService(
            database.Context,
            null!,
            dateTimeProvider,
            new RecordingAuditLogService(),
            null!,
            NullLogger<ServiceRecordService>.Instance);
    }

    private static CreateServiceRecordRequest CreateRequest(string? clientRequestId, string plate)
    {
        return new CreateServiceRecordRequest
        {
            ClientRequestId = clientRequestId,
            CustomerName = $"Test Customer {plate}",
            CustomerPhoneNumber = CreatePhoneFromPlate(plate),
            CustomerType = CustomerType.Individual,
            Plate = plate,
            RequestItems =
            [
                new CreateServiceRequestItemDto { Title = "Bakım" }
            ]
        };
    }

    private static string CreatePhoneFromPlate(string plate)
    {
        var digits = new string(plate.Where(char.IsDigit).ToArray());
        return $"050{digits.PadRight(8, '0')}"[..11];
    }

    private static void AddWorkshop(TestDatabase database, int id)
    {
        database.Context.Workshops.Add(new AutoStock.Repositories.Entities.Workshop
        {
            Id = id,
            Name = $"Workshop {id}",
            CreatedAt = DateTime.UtcNow,
            SubscriptionStartDate = DateTime.UtcNow
        });
    }

    private static string FindWorkspaceFile(params string[] relativeParts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(new[] { directory.FullName }.Concat(relativeParts).ToArray());

            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Workspace file could not be found.", Path.Combine(relativeParts));
    }
}
