using AutoStock.Services.Interfaces;
using AutoStock.Services.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AutoStock.Services.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddScoped<IPdfService, PdfService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<JwtService>();
            services.AddScoped<IServiceRecordService, ServiceRecordService>();

            return services;


        }
    }
}
