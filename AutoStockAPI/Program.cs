using AutoStock.API.Extensions;
using AutoStock.API.Middlewares;
using AutoStock.Repositories.Extensions;
using AutoStock.Services.Extensions;
using AutoStock.Services.Validators.Customers;
using FluentValidation;
using FluentValidation.AspNetCore;
using Serilog;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "AutoStock.API");
});
builder.Services.AddValidatorsFromAssemblyContaining<CreateCustomerDtoValidator>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddControllers();
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("public-qr", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();



builder.Services.AddRepositories(builder.Configuration)
    .AddServices(builder.Configuration)
    .AddIdentityServices()
    .AddJwtAuth(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

var app = builder.Build();

await app.SeedRolesAsync();


app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseMiddleware<AuditContextMiddleware>();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

app.Run();
