using EShop.Web;
using EShop.Web.Components;
using EShop.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// CHG-20261106-001-SHOPPING-CART
// Change: Uses Aspire service discovery for API clients and circuit-scoped cart state.
builder.Services.AddHttpClient<ProductApiClient>(client =>
    client.BaseAddress = new Uri("https+http://apiservice"));
builder.Services.AddHttpClient<OrderApiClient>(client =>
    client.BaseAddress = new Uri("https+http://apiservice"));
builder.Services.AddScoped<CartState>();

builder.Services.AddOutputCache();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
