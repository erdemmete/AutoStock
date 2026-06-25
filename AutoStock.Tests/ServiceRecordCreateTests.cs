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
        database.Context.Workshops.Add(new AutoStock.Repositories.Entities.Workshop
        {
            Id = 1,
            Name = "Workshop",
            CreatedAt = DateTime.UtcNow,
            SubscriptionStartDate = DateTime.UtcNow
        });
        await database.Context.SaveChangesAsync();

        var service = new ServiceRecordService(
            database.Context,
            null!,
            new MutableDateTimeProvider(new DateTime(2026, 6, 24, 12, 0, 0, DateTimeKind.Utc)),
            new RecordingAuditLogService(),
            null!,
            NullLogger<ServiceRecordService>.Instance);
        var request = new CreateServiceRecordRequest
        {
            ClientRequestId = "request-123",
            CustomerName = "Test Customer",
            CustomerPhoneNumber = "05000000000",
            CustomerType = CustomerType.Individual,
            Plate = "34TEST123",
            RequestItems =
            [
                new CreateServiceRequestItemDto { Title = "Bakım" }
            ]
        };

        var first = await service.CreateAsync(request, 1);
        var second = await service.CreateAsync(request, 1);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Data!.ServiceRecordId, second.Data!.ServiceRecordId);
        Assert.Single(database.Context.ServiceRecords);
    }
}
