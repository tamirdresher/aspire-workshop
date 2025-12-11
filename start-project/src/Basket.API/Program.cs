using Basket.API.Services;
using ECommerce.Shared.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddOpenApi();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// PROBLEM: Manual HttpClient configuration for service-to-service communication
// We need to configure an HttpClient for every service dependency
// This is error-prone and requires updating configurations across environments
builder.Services.AddHttpClient<CatalogClient>();

// Add basket service
builder.Services.AddSingleton<BasketService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseHttpsRedirection();

// Basket endpoints
app.MapGet("/api/basket/{buyerId}", async (string buyerId, BasketService basketService) =>
{
    var basket = await basketService.GetBasketAsync(buyerId);
    return basket is not null ? Results.Ok(basket) : Results.Ok(new CustomerBasket { BuyerId = buyerId });
})
.WithName("GetBasket")
.WithOpenApi();

app.MapPost("/api/basket", async (CustomerBasket basket, BasketService basketService) =>
{
    var updated = await basketService.UpdateBasketAsync(basket);
    return Results.Ok(updated);
})
.WithName("UpdateBasket")
.WithOpenApi();

app.MapDelete("/api/basket/{buyerId}", async (string buyerId, BasketService basketService) =>
{
    await basketService.DeleteBasketAsync(buyerId);
    return Results.NoContent();
})
.WithName("DeleteBasket")
.WithOpenApi();

app.MapPost("/api/basket/{buyerId}/checkout", async (string buyerId, string shippingAddress, BasketService basketService) =>
{
    var success = await basketService.CheckoutBasketAsync(buyerId, shippingAddress);
    return success ? Results.Ok(new { Message = "Checkout initiated" }) : Results.BadRequest(new { Error = "Basket not found" });
})
.WithName("CheckoutBasket")
.WithOpenApi();

app.Run();
