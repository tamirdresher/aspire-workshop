using Ordering.API.Services;
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

// Add ordering service
builder.Services.AddSingleton<OrderingService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseHttpsRedirection();

// Order endpoints
app.MapGet("/api/orders", async (OrderingService orderingService, string? buyerId = null) =>
{
    var orders = await orderingService.GetOrdersAsync(buyerId);
    return Results.Ok(orders);
})
.WithName("GetOrders")
.WithOpenApi();

app.MapGet("/api/orders/{id}", async (string id, OrderingService orderingService) =>
{
    var order = await orderingService.GetOrderByIdAsync(id);
    return order is not null ? Results.Ok(order) : Results.NotFound();
})
.WithName("GetOrderById")
.WithOpenApi();

app.MapPost("/api/orders", async (Order order, OrderingService orderingService) =>
{
    var created = await orderingService.CreateOrderAsync(order);
    return Results.Created($"/api/orders/{created.Id}", created);
})
.WithName("CreateOrder")
.WithOpenApi();

app.MapPut("/api/orders/{id}/status", async (string id, string status, OrderingService orderingService) =>
{
    var order = await orderingService.UpdateOrderStatusAsync(id, status);
    return order is not null ? Results.Ok(order) : Results.NotFound();
})
.WithName("UpdateOrderStatus")
.WithOpenApi();

app.Run();
