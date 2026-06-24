using AutoStock.WEB.Extensions;
using AutoStock.WEB.ModelBinders;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

var supportedCultures = new[]
{
    new CultureInfo("tr-TR")
};

CultureInfo.DefaultThreadCurrentCulture = supportedCultures[0];
CultureInfo.DefaultThreadCurrentUICulture = supportedCultures[0];

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    options.ModelBinderProviders.Insert(0, new FlexibleDecimalModelBinderProvider());
});

builder.Services.AddHttpClient();

builder.Services.AddSession();

builder.Services.AddWebServices(builder.Configuration);

var app = builder.Build();

//app.Urls.Add("http://0.0.0.0:5018");

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("tr-TR"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    if (context.Request.Host.Host.Equals("www.sente360.com", StringComparison.OrdinalIgnoreCase))
    {
        var canonicalUrl = $"https://sente360.com{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}";
        context.Response.Redirect(canonicalUrl, permanent: true, preserveMethod: true);
        return;
    }

    await next();
});

app.UseHttpsRedirection();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
