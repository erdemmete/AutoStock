using AutoStock.Services.Interfaces;
using AutoStock.Services.Options;
using AutoStock.Services.Services;
using AutoStock.WEB.Services;

namespace AutoStock.WEB.Extensions
{
    public static class WebServiceExtensions
    {
        public static IServiceCollection AddWebServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddHttpContextAccessor();

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
            services.AddScoped<InvoiceExportApiService>();
            services.AddScoped<InvoiceExportPageService>();
            services.AddScoped<CurrentAccountApiService>();
            services.AddScoped<CurrentAccountPageService>();
            services.AddScoped<SupportRequestApiService>();
            services.AddScoped<AuthApiService>();
            services.AddScoped<DashboardApiService>();
            services.AddScoped<VehicleQrCodesApiService>();

            services.Configure<EmailSettings>(
                configuration.GetSection("EmailSettings"));

            services.AddScoped<IEmailSender, SmtpEmailSender>();
            services.AddScoped<AdminWorkshopInviteEmailService>();
            services.AddScoped<AccountingInvoiceRequestApiService>();

            return services;
        }
    }
}
