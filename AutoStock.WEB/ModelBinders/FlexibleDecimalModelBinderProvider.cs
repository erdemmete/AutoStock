using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AutoStock.WEB.ModelBinders
{
    public class FlexibleDecimalModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var modelType = context.Metadata.ModelType;
            var underlyingType = Nullable.GetUnderlyingType(modelType);

            if (modelType == typeof(decimal) || underlyingType == typeof(decimal))
            {
                return new FlexibleDecimalModelBinder();
            }

            return null;
        }
    }
}