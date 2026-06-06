using AutoStock.WEB.Services;

namespace AutoStock.WEB.Extensions
{
    public static class WebServiceExtensions
    {
        public static IServiceCollection AddWebServices(
            this IServiceCollection services)
        {
            services.AddHttpContextAccessor();

            services.AddScoped<CurrentAccountApiService>();
            services.AddScoped<StockItemApiService>();
            services.AddScoped<AdminWorkshopApiService>();
            services.AddScoped<StockItemPageService>();
            services.AddScoped<AdminWorkshopPageService>();
            services.AddScoped<ServiceRecordApiService>();
            services.AddScoped<ServiceRecordPageService>();
            services.AddScoped<CustomerApiService>();
            services.AddScoped<CustomerPageService>();
            services.AddScoped<InvoiceApiService>();
            services.AddScoped<InvoicePageService>();
            services.AddScoped<CurrentAccountApiService>();
            services.AddScoped<CurrentAccountPageService>();

            return services;
        }
    }
}