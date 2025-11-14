using Microsoft.Azure.Cosmos;
using Catalog.API.Services;
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

// Add Cosmos DB client
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connectionString = config["CosmosDb:ConnectionString"] ?? "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
    return new CosmosClient(connectionString);
});

// Add catalog service
builder.Services.AddScoped<CatalogService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseHttpsRedirection();

// Catalog endpoints
app.MapGet("/api/catalog", async (CatalogService catalogService, int page = 1, int pageSize = 10) =>
{
    var items = await catalogService.GetItemsAsync(page, pageSize);
    return Results.Ok(items);
})
.WithName("GetCatalogItems")
.WithOpenApi();

app.MapGet("/api/catalog/{id}", async (string id, CatalogService catalogService) =>
{
    var item = await catalogService.GetItemByIdAsync(id);
    return item is not null ? Results.Ok(item) : Results.NotFound();
})
.WithName("GetCatalogItemById")
.WithOpenApi();

app.MapGet("/api/catalog/category/{category}", async (string category, CatalogService catalogService) =>
{
    var items = await catalogService.GetItemsByCategoryAsync(category);
    return Results.Ok(items);
})
.WithName("GetCatalogItemsByCategory")
.WithOpenApi();

app.MapPost("/api/catalog", async (CatalogItem item, CatalogService catalogService) =>
{
    var created = await catalogService.CreateItemAsync(item);
    return Results.Created($"/api/catalog/{created.Id}", created);
})
.WithName("CreateCatalogItem")
.WithOpenApi();

app.MapPut("/api/catalog/{id}", async (string id, CatalogItem item, CatalogService catalogService) =>
{
    item.Id = id;
    var updated = await catalogService.UpdateItemAsync(item);
    return Results.Ok(updated);
})
.WithName("UpdateCatalogItem")
.WithOpenApi();

app.MapDelete("/api/catalog/{id}", async (string id, CatalogService catalogService) =>
{
    await catalogService.DeleteItemAsync(id);
    return Results.NoContent();
})
.WithName("DeleteCatalogItem")
.WithOpenApi();

app.Run();
