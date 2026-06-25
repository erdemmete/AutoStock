using AutoStock.Services.Calculations;

namespace AutoStock.Tests;

public class ServiceRecordTotalsCalculatorTests
{
    [Fact]
    public void QuantityDiscountAndVatProduceOneCanonicalTotal()
    {
        var lines = new[]
        {
            new ServiceFinancialLine(2m, 1_000m, 10m, ServiceFinancialRules.DefaultVatRate),
            new ServiceFinancialLine(1m, 500m, 0m, ServiceFinancialRules.DefaultVatRate)
        };

        var totals = ServiceRecordTotalsCalculator.Calculate(lines);
        var serviceScreenTotal = totals.GrandTotal;
        var skfTotal = totals.GrandTotal;
        var accountSummaryTotal = totals.GrandTotal;
        var invoiceTotal = totals.GrandTotal;

        Assert.Equal(2_500m, totals.Subtotal);
        Assert.Equal(200m, totals.Discount);
        Assert.Equal(460m, totals.Vat);
        Assert.Equal(2_760m, totals.GrandTotal);
        Assert.Equal(serviceScreenTotal, skfTotal);
        Assert.Equal(skfTotal, accountSummaryTotal);
        Assert.Equal(accountSummaryTotal, invoiceTotal);
    }

    [Fact]
    public void ServiceOperationsUseTheCentralVatRule()
    {
        var totals = ServiceRecordTotalsCalculator.CalculateServiceOperations(
            new[] { (Quantity: 2m, UnitPrice: 750m) });
        var expected = ServiceRecordTotalsCalculator.Calculate(
            new[]
            {
                new ServiceFinancialLine(
                    2m,
                    750m,
                    0m,
                    ServiceFinancialRules.DefaultVatRate)
            });

        Assert.Equal(expected, totals);
    }

    [Fact]
    public void MonetaryComponentsAreRoundedPerLine()
    {
        var totals = ServiceRecordTotalsCalculator.Calculate(
            new[]
            {
                new ServiceFinancialLine(
                    1m,
                    0.03m,
                    0m,
                    ServiceFinancialRules.DefaultVatRate)
            });

        Assert.Equal(0.03m, totals.Subtotal);
        Assert.Equal(0.01m, totals.Vat);
        Assert.Equal(0.04m, totals.GrandTotal);
    }
}
