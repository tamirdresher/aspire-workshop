using Bookstore.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Azure.Storage.Queues;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Bookstore.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Aspire.Microsoft.EntityFrameworkCore.Cosmos;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add Redis output caching
builder.AddRedisOutputCache("cache");

// Add Health Checks UI to the API
builder.AddHealthChecksUI();

// Add Azure CosmosDB client via Aspire
builder.AddCosmosDbContext<BookstoreDbContext>("cosmos");

// Add Repository
builder.Services.AddScoped<BookstoreRepository>();
builder.Services.AddScoped<OrderRepository>();

// Add Azure Storage Queue client via Aspire
builder.AddAzureQueueServiceClient("queue");

builder.Services.AddSingleton<QueueClient>(sp =>
{
    var serviceClient = sp.GetRequiredService<QueueServiceClient>();
    return serviceClient.GetQueueClient("book-created");
});

//Add specific health checks for the API
builder.Services.AddHealthChecks()
   // Custom health check example
   .AddCheck("api-health", () =>
   {
       // Custom logic to check API health
       return HealthCheckResult.Healthy("API is running smoothly");
   }, tags: new[] { "api", "ready" });

builder.Services.AddOpenApi();
builder.Services.AddOutputCache();
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

app.UseOutputCache();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapDefaultEndpoints();

app.UseCors();
app.UseHttpsRedirection();

// Initialize Cosmos DB
Console.WriteLine("Initializing Cosmos DB...");
CosmosClient cosmosClient;
QueueClient queueClient;

try
{
   
    // Initialize Azure Storage Queue
    Console.WriteLine("Initializing Azure Storage Queue...");
    queueClient = app.Services.GetRequiredService<QueueClient>();
    Console.WriteLine("Azure Storage Queue client created.");
}
catch (Exception ex)
{
    Console.WriteLine($"CRITICAL ERROR during startup initialization: {ex}");
    throw;
}


try
{
   

    // Ensure database creation for EF Core
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<BookstoreDbContext>();
        await context.Database.EnsureCreatedAsync();
    }


    await queueClient.CreateIfNotExistsAsync();
    Console.WriteLine("Queue created/found.");

    // Seed initial books
    // await SeedBooks();
}
catch (Exception ex)
{
    Console.WriteLine($"Error initializing Cosmos DB: {ex}");
}

app.MapPost("/seed", async (BookstoreRepository repository) =>
{
    var existingBooks = await repository.GetBooksAsync();

    if (existingBooks.Count == 0)
    {
        var initialBooks = new[]
        {
            new Book { Id = Guid.NewGuid().ToString(), Title = "The Great Gatsby", Author = "F. Scott Fitzgerald", Price = 12.99m, Description = "Pending AI generation...", Stock = 15, ImageUrl = "https://picsum.photos/200/300?text=The+Great+Gatsby" },
            new Book { Id = Guid.NewGuid().ToString(), Title = "1984", Author = "George Orwell", Price = 10.99m, Description = "Pending AI generation...", Stock = 20, ImageUrl = "https://picsum.photos/200/300?text=1984" },
            new Book { Id = Guid.NewGuid().ToString(), Title = "To Kill a Mockingbird", Author = "Harper Lee", Price = 14.99m, Description = "Pending AI generation...", Stock = 12, ImageUrl = "https://picsum.photos/200/300?text=To+Kill+a+Mockingbird" },
            new Book { Id = Guid.NewGuid().ToString(), Title = "Pride and Prejudice", Author = "Jane Austen", Price = 11.99m, Description = "Pending AI generation...", Stock = 18, ImageUrl = "https://picsum.photos/200/300?text=Pride+and+Prejudice" },
            new Book { Id = Guid.NewGuid().ToString(), Title = "The Catcher in the Rye", Author = "J.D. Salinger", Price = 13.99m, Description = "Pending AI generation...", Stock = 10, ImageUrl = "https://picsum.photos/200/300?text=Catcher+in+the+Rye" }
        };

        foreach (var book in initialBooks)
        {
            await repository.CreateBookAsync(book);
        }
        return Results.Ok("Database seeded successfully.");
    }
    return Results.Ok("Database already contains data.");
})
.WithName("SeedDatabase");

// Books endpoints
app.MapGet("/books", async (BookstoreRepository repository) =>
{
    var books = await repository.GetBooksAsync();
    return Results.Ok(books);
})
.CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(5)))
.WithName("GetBooks");

app.MapGet("/books/{id}", async (string id, BookstoreRepository repository) =>
{
    var book = await repository.GetBookAsync(id);
    return book is not null ? Results.Ok(book) : Results.NotFound();
})
.WithName("GetBook");

app.MapPost("/books", async ([FromBody] Book book, BookstoreRepository repository) =>
{
    if (string.IsNullOrEmpty(book.Id))
    {
        book.Id = Guid.NewGuid().ToString();
    }

    book.Description = "Pending AI generation...";
    await repository.CreateBookAsync(book);

    // Publish message to Azure Storage Queue for AI description generation
    var message = new BookCreatedMessage
    {
        BookId = book.Id,
        Title = book.Title,
        Author = book.Author
    };

    var messageJson = JsonSerializer.Serialize(message);
    await queueClient.SendMessageAsync(messageJson);

    return Results.Created($"/books/{book.Id}", book);
})
.WithName("CreateBook");

app.MapPut("/books/{id}", async (string id, [FromBody] Book updatedBook, BookstoreRepository repository) =>
{
    await repository.UpdateBookAsync(id, updatedBook);
    return Results.Ok(updatedBook);
})
.WithName("UpdateBook");

app.MapDelete("/books/{id}", async (string id, BookstoreRepository repository) =>
{
    await repository.DeleteBookAsync(id);
    return Results.NoContent();
})
.WithName("DeleteBook");

// Cart endpoints
app.MapGet("/cart/{cartId}", async (string cartId, BookstoreRepository repository) =>
{
    var cart = await repository.GetCartAsync(cartId);
    return Results.Ok(cart);
})
.WithName("GetCart");

app.MapPost("/cart/{cartId}/items", async (string cartId, [FromBody] CartItem item, BookstoreRepository repository) =>
{
    var cart = await repository.AddToCartAsync(cartId, item);
    return Results.Ok(cart);
})
.WithName("AddToCart");

app.MapDelete("/cart/{cartId}/items/{bookId}", async (string cartId, string bookId, BookstoreRepository repository) =>
{
    var cart = await repository.RemoveFromCartAsync(cartId, bookId);
    return Results.Ok(cart);
})
.WithName("RemoveFromCart");

app.MapDelete("/cart/{cartId}", async (string cartId, BookstoreRepository repository) =>
{
    await repository.ClearCartAsync(cartId);
    return Results.NoContent();
})
.WithName("ClearCart");

// Order endpoints
app.MapGet("/orders", async (OrderRepository repository) =>
{
    var orders = await repository.GetOrdersAsync();
    return Results.Ok(orders);
})
.WithName("GetOrders");

app.MapGet("/orders/{id}", async (string id, OrderRepository repository) =>
{
    var order = await repository.GetOrderAsync(id);
    return order is not null ? Results.Ok(order) : Results.NotFound();
})
.WithName("GetOrder");

app.MapPost("/orders", async ([FromBody] Order order, OrderRepository repository) =>
{
    if (string.IsNullOrEmpty(order.Id))
    {
        order.Id = Guid.NewGuid().ToString();
    }

    order.OrderDate = DateTime.UtcNow;
    order.TotalAmount = order.Items.Sum(i => i.TotalPrice);
    await repository.CreateOrderAsync(order);

    return Results.Created($"/orders/{order.Id}", order);
})
.WithName("CreateOrder");

// Worker endpoint to update book descriptions
app.MapPut("/books/{id}/description", async (string id, [FromBody] string description, BookstoreRepository repository) =>
{
    var book = await repository.GetBookAsync(id);
    if (book is null)
    {
        return Results.NotFound();
    }

    book.Description = description;
    await repository.UpdateBookAsync(id, book);
    return Results.Ok(book);
})
.WithName("UpdateBookDescription");

app.Run();
