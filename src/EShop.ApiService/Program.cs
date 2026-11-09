using EShop.ApiService.Contracts;
using EShop.ApiService.Endpoints;
using EShop.ApiService.Middleware;
using EShop.ApiService.Services;
using EShop.Infrastructure;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DatabaseConcurrencyExceptionHandler>();
builder.Services.AddValidation();
// CHG-20261106-003-DATABASE-INFRASTRUCTURE
// Change: Uses scoped database-backed services and applies migrations before serving requests.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");
builder.Services.AddEShopInfrastructure(connectionString, builder.Environment.ContentRootPath);
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();
app.UseStatusCodePages();
await app.Services.InitializeDatabaseAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapDefaultEndpoints();
app.MapProductEndpoints();
app.MapOrderEndpoints();

app.Run();

// CHG-20261106-001-SHOPPING-CART
// Purpose: Makes the API entry point available to integration-test hosting.
public partial class Program
{
}
