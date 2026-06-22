using AutoStock.Services.Interfaces;
using AutoStock.Services.Options;
using AutoStock.Services.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Services.Interfaces.StockItems;
using Services.Services.StockItems;

namespace AutoStock.Services.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddScoped<IPdfService, PdfService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IAccountService, AccountService>();

            services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

            services.AddScoped<JwtService>();
            services.AddScoped<IServiceRecordService, ServiceRecordService>();
            services.AddScoped<IVehicleCatalogService, VehicleCatalogService>();
            services.AddScoped<IInvoiceService, InvoiceService>();
            services.AddScoped<IInvoiceExportService, InvoiceExportService>();
            services.AddScoped<ICurrentAccountService, CurrentAccountService>();
            services.AddScoped<IStockItemService, StockItemService>();
            services.AddScoped<IAdminWorkshopService, AdminWorkshopService>();
            services.AddScoped<IAuditLogService, AuditLogService>();
            services.AddScoped<IAuditContextAccessor, AuditContextAccessor>();
            services.AddScoped<ISupportRequestService, SupportRequestService>();
            services.AddScoped<IUserSecurityTokenService, UserSecurityTokenService>();
            services.AddScoped<IVehicleService, VehicleService>();
            services.AddScoped<IServiceRecordImageService, ServiceRecordImageService>();
            services.Configure<EmailSettings>(
                configuration.GetSection("EmailSettings"));
            services.Configure<WebPushSettings>(
                configuration.GetSection("WebPushSettings"));

            services.AddScoped<IEmailSender, SmtpEmailSender>();
            services.AddScoped<IInvoiceEmailService, InvoiceEmailService>();
            services.AddScoped<IVehicleCatalogSeeder, VehicleCatalogSeeder>();
            services.AddScoped<IAccountingInvoiceRequestService, AccountingInvoiceRequestService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IWebPushSubscriptionService, WebPushSubscriptionService>();
            services.AddScoped<IWebPushSender, WebPushSender>();
            services.AddScoped<IEntityEditLockService, EntityEditLockService>();

            return services;
        }
    }
}
