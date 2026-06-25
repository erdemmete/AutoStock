namespace AutoStock.Services.Calculations
{
    public static class ServiceFinancialRules
    {
        public const decimal DefaultVatRate = 20m;
    }

    public readonly record struct ServiceFinancialLine(
        decimal Quantity,
        decimal UnitPrice,
        decimal DiscountRate,
        decimal VatRate);

    public readonly record struct ServiceFinancialLineTotal(
        decimal Subtotal,
        decimal Discount,
        decimal Vat,
        decimal GrandTotal);

    public readonly record struct ServiceFinancialTotals(
        decimal Subtotal,
        decimal Discount,
        decimal Vat,
        decimal GrandTotal);

    public static class ServiceRecordTotalsCalculator
    {
        public static ServiceFinancialLineTotal CalculateLine(ServiceFinancialLine line)
        {
            var quantity = Math.Max(0m, line.Quantity);
            var unitPrice = Math.Max(0m, line.UnitPrice);
            var discountRate = Math.Max(0m, line.DiscountRate);
            var vatRate = Math.Max(0m, line.VatRate);

            var subtotal = RoundMoney(quantity * unitPrice);
            var discount = RoundMoney(subtotal * discountRate / 100m);
            var taxable = subtotal - discount;
            var vat = RoundMoney(taxable * vatRate / 100m);

            return new ServiceFinancialLineTotal(
                subtotal,
                discount,
                vat,
                RoundMoney(taxable + vat));
        }

        public static ServiceFinancialTotals Calculate(IEnumerable<ServiceFinancialLine> lines)
        {
            decimal subtotal = 0m;
            decimal discount = 0m;
            decimal vat = 0m;
            decimal grandTotal = 0m;

            foreach (var line in lines)
            {
                var result = CalculateLine(line);
                subtotal += result.Subtotal;
                discount += result.Discount;
                vat += result.Vat;
                grandTotal += result.GrandTotal;
            }

            return new ServiceFinancialTotals(subtotal, discount, vat, grandTotal);
        }

        private static decimal RoundMoney(decimal value)
        {
            return decimal.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        public static ServiceFinancialTotals CalculateServiceOperations(
            IEnumerable<(decimal Quantity, decimal UnitPrice)> operations)
        {
            return Calculate(operations.Select(x => new ServiceFinancialLine(
                x.Quantity,
                x.UnitPrice,
                0m,
                ServiceFinancialRules.DefaultVatRate)));
        }
    }
}
