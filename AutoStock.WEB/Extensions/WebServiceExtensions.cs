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

            return services;
        }
    }
}