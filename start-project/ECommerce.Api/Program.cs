using ECommerce.Api.Services;
using ECommerce.Shared.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddSingleton<ProductService>();
builder.Services.AddSingleton<OrderService>();

// Add CORS for web frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseHttpsRedirection();

// Product endpoints
app.MapGet("/api/products", async (ProductService productService) =>
{
    var products = await productService.GetAllProductsAsync();
    return Results.Ok(products);
})
.WithName("GetAllProducts")
.WithOpenApi();

app.MapGet("/api/products/{id}", async (int id, ProductService productService) =>
{
    var product = await productService.GetProductByIdAsync(id);
    return product is not null ? Results.Ok(product) : Results.NotFound();
})
.WithName("GetProductById")
.WithOpenApi();

app.MapGet("/api/products/category/{category}", async (string category, ProductService productService) =>
{
    var products = await productService.GetProductsByCategoryAsync(category);
    return Results.Ok(products);
})
.WithName("GetProductsByCategory")
.WithOpenApi();

// Order endpoints
app.MapGet("/api/orders", async (OrderService orderService) =>
{
    var orders = await orderService.GetAllOrdersAsync();
    return Results.Ok(orders);
})
.WithName("GetAllOrders")
.WithOpenApi();

app.MapGet("/api/orders/{id}", async (int id, OrderService orderService) =>
{
    var order = await orderService.GetOrderByIdAsync(id);
    return order is not null ? Results.Ok(order) : Results.NotFound();
})
.WithName("GetOrderById")
.WithOpenApi();

app.MapPost("/api/orders", async (Order order, OrderService orderService) =>
{
    var createdOrder = await orderService.CreateOrderAsync(order);
    return Results.Created($"/api/orders/{createdOrder.Id}", createdOrder);
})
.WithName("CreateOrder")
.WithOpenApi();

app.MapPut("/api/orders/{id}/status", async (int id, string status, OrderService orderService) =>
{
    var order = await orderService.UpdateOrderStatusAsync(id, status);
    return order is not null ? Results.Ok(order) : Results.NotFound();
})
.WithName("UpdateOrderStatus")
.WithOpenApi();

app.Run();
