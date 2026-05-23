using AutoStock.API.Extensions;
using AutoStock.API.Middlewares;
using AutoStock.Repositories.Extensions;
using AutoStock.Services.Extensions;
using FluentValidation;
using FluentValidation.AspNetCore;
using AutoStock.Services.Validators.Customers;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddValidatorsFromAssemblyContaining<CreateCustomerDtoValidator>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddControllers();


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
app.UseAuthorization();

app.MapControllers();

app.Run();