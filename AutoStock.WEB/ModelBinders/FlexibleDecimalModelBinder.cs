using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Globalization;

namespace AutoStock.WEB.ModelBinders
{
    public class FlexibleDecimalModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
                throw new ArgumentNullException(nameof(bindingContext));

            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);

            if (valueProviderResult == ValueProviderResult.None)
                return Task.CompletedTask;

            bindingContext.ModelState.SetModelValue(
                bindingContext.ModelName,
                valueProviderResult);

            var rawValue = valueProviderResult.FirstValue;

            if (string.IsNullOrWhiteSpace(rawValue))
            {
                if (Nullable.GetUnderlyingType(bindingContext.ModelType) != null)
                {
                    bindingContext.Result = ModelBindingResult.Success(null);
                }

                return Task.CompletedTask;
            }

            if (TryParseDecimal(rawValue, out var decimalValue))
            {
                bindingContext.Result = ModelBindingResult.Success(decimalValue);
                return Task.CompletedTask;
            }

            bindingContext.ModelState.TryAddModelError(
                bindingContext.ModelName,
                "Geçerli bir sayı giriniz.");

            return Task.CompletedTask;
        }

        private static bool TryParseDecimal(string rawValue, out decimal result)
        {
            result = 0;

            var value = rawValue
                .Trim()
                .Replace("₺", "")
                .Replace("TL", "", StringComparison.OrdinalIgnoreCase)
                .Replace(" ", "");

            if (string.IsNullOrWhiteSpace(value))
                return false;

            var hasComma = value.Contains(',');
            var hasDot = value.Contains('.');

            string normalized;

            if (hasComma && hasDot)
            {
                var lastCommaIndex = value.LastIndexOf(',');
                var lastDotIndex = value.LastIndexOf('.');

                if (lastCommaIndex > lastDotIndex)
                {
                    // Örnek: 20.000,50
                    normalized = value.Replace(".", "").Replace(",", ".");
                }
                else
                {
                    // Örnek: 20,000.50
                    normalized = value.Replace(",", "");
                }
            }
            else if (hasComma)
            {
                // Örnek: 20,00
                normalized = value.Replace(".", "").Replace(",", ".");
            }
            else if (hasDot)
            {
                var parts = value.Split('.');

                if (parts.Length == 2 && parts[1].Length <= 2)
                {
                    // Örnek: 20.00
                    normalized = value;
                }
                else
                {
                    // Örnek: 20.000
                    normalized = value.Replace(".", "");
                }
            }
            else
            {
                normalized = value;
            }

            return decimal.TryParse(
                normalized,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out result);
        }
    }
}