using AutoStock.Repositories.Entities;
using AutoStock.Repositories.Enums;
using AutoStock.Services.Calculations;
using AutoStock.Services.Dtos.Invoices;
using AutoStock.Services.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoStock.Tests;

public class InvoiceActiveConstraintTests
{
    [Fact]
    public async Task ActiveInvoiceIndexIsUniqueAndFilteredToDraftAndIssued()
    {
        await using var database = await TestDatabase.CreateAsync();
        var invoiceType = database.Context.Model.FindEntityType(typeof(Invoice));
        var index = invoiceType!
            .GetIndexes()
            .Single(x => x.GetDatabaseName() == "UX_Invoices_Active_ServiceRecordId");

        Assert.True(index.IsUnique);
        Assert.Equal(nameof(Invoice.ServiceRecordId), Assert.Single(index.Properties).Name);
        Assert.Equal(
            "[ServiceRecordId] IS NOT NULL AND [Status] IN (1, 2)",
            index.GetFilter());
        Assert.Equal(1, (int)InvoiceStatus.Draft);
        Assert.Equal(2, (int)InvoiceStatus.Issued);
        Assert.Equal(3, (int)InvoiceStatus.Cancelled);
    }

    [Fact]
    public async Task ServiceRecordClientRequestIndexIsUniquePerWorkshop()
    {
        await using var database = await TestDatabase.CreateAsync();
        var serviceRecordType = database.Context.Model.FindEntityType(typeof(ServiceRecord));
        var index = serviceRecordType!
            .GetIndexes()
            .Single(x => x.Properties.Select(p => p.Name)
                .SequenceEqual([nameof(ServiceRecord.WorkshopId), nameof(ServiceRecord.ClientRequestId)]));

        Assert.True(index.IsUnique);
        Assert.Equal("[ClientRequestId] IS NOT NULL", index.GetFilter());
    }

    [Fact]
    public async Task RepeatedCreateReturnsTheExistingActiveInvoice()
    {
        await using var database = await TestDatabase.CreateAsync();
        var seeded = await database.SeedServiceRecordAsync();
        var service = CreateService(database);
        var request = CreateRequest(seeded);

        var first = await service.CreateAsync(request, seeded.Workshop.Id);
        var second = await service.CreateAsync(request, seeded.Workshop.Id);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Data!.InvoiceId, second.Data!.InvoiceId);
        Assert.Single(database.Context.Invoices);
    }

    [Fact]
    public async Task CancelledInvoiceAllowsANewDraft()
    {
        await using var database = await TestDatabase.CreateAsync();
        var seeded = await database.SeedServiceRecordAsync();
        var service = CreateService(database);
        var request = CreateRequest(seeded);
        var first = await service.CreateAsync(request, seeded.Workshop.Id);

        var firstInvoice = await database.Context.Invoices.FindAsync(first.Data!.InvoiceId);
        firstInvoice!.Status = InvoiceStatus.Cancelled;
        await database.Context.SaveChangesAsync();

        var second = await service.CreateAsync(request, seeded.Workshop.Id);

        Assert.True(second.IsSuccess);
        Assert.NotEqual(first.Data.InvoiceId, second.Data!.InvoiceId);
        Assert.Equal(2, database.Context.Invoices.Count());
    }

    private static InvoiceService CreateService(TestDatabase database)
    {
        return new InvoiceService(
            database.Context,
            null!,
            new MutableDateTimeProvider(new DateTime(2026, 6, 24, 12, 0, 0, DateTimeKind.Utc)),
            new RecordingAuditLogService(),
            NullLogger<InvoiceService>.Instance);
    }

    private static CreateInvoiceDto CreateRequest(SeededRecord seeded)
    {
        return new CreateInvoiceDto
        {
            ServiceRecordId = seeded.ServiceRecord.Id,
            CustomerId = seeded.Customer.Id,
            CustomerTitle = seeded.Customer.FullName,
            Plate = seeded.Vehicle.Plate,
            Items =
            [
                new CreateInvoiceItemDto
                {
                    ItemType = (int)InvoiceItemType.Labor,
                    Description = "Bakım",
                    Quantity = 2,
                    UnitPrice = 500,
                    VatRate = ServiceFinancialRules.DefaultVatRate
                }
            ]
        };
    }
}
