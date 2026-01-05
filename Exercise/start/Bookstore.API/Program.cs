using Bookstore.Shared;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseHttpsRedirection();

// In-memory data stores
var books = new ConcurrentDictionary<string, Book>();
var carts = new ConcurrentDictionary<string, Cart>();
var orders = new ConcurrentDictionary<string, Order>();
var messageQueue = new ConcurrentQueue<BookCreatedMessage>();

// Seed initial books
SeedBooks();

void SeedBooks()
{
    var initialBooks = new[]
    {
        new Book { Id = Guid.NewGuid().ToString(), Title = "The Great Gatsby", Author = "F. Scott Fitzgerald", Price = 12.99m, Description = "Pending AI generation...", Stock = 15, ImageUrl = "https://loremflickr.com/200/300/The+Great+Gatsby" },
        new Book { Id = Guid.NewGuid().ToString(), Title = "1984", Author = "George Orwell", Price = 10.99m, Description = "Pending AI generation...", Stock = 20, ImageUrl = "https://loremflickr.com/200/300/1984" },
        new Book { Id = Guid.NewGuid().ToString(), Title = "To Kill a Mockingbird", Author = "Harper Lee", Price = 14.99m, Description = "Pending AI generation...", Stock = 12, ImageUrl = "https://loremflickr.com/200/300/To+Kill+a+Mockingbird" },
        new Book { Id = Guid.NewGuid().ToString(), Title = "Pride and Prejudice", Author = "Jane Austen", Price = 11.99m, Description = "Pending AI generation...", Stock = 18, ImageUrl = "https://loremflickr.com/200/300/Pride+and+Prejudice" },
        new Book { Id = Guid.NewGuid().ToString(), Title = "The Catcher in the Rye", Author = "J.D. Salinger", Price = 13.99m, Description = "Pending AI generation...", Stock = 10, ImageUrl = "https://loremflickr.com/200/300/Catcher+in+the+Rye" }
    };

    foreach (var book in initialBooks)
    {
        books.TryAdd(book.Id, book);
    }
}

// Books endpoints
app.MapGet("/books", () =>
{
    return Results.Ok(books.Values.ToList());
})
.WithName("GetBooks");

app.MapGet("/books/{id}", (string id) =>
{
    return books.TryGetValue(id, out var book)
        ? Results.Ok(book)
        : Results.NotFound();
})
.WithName("GetBook");

app.MapPost("/books", ([FromBody] Book book) =>
{
    if (string.IsNullOrEmpty(book.Id))
    {
        book.Id = Guid.NewGuid().ToString();
    }
    
    book.Description = "Pending AI generation...";
    books.TryAdd(book.Id, book);
    
    // Publish message to queue for AI description generation
    messageQueue.Enqueue(new BookCreatedMessage
    {
        BookId = book.Id,
        Title = book.Title,
        Author = book.Author
    });
    
    return Results.Created($"/books/{book.Id}", book);
})
.WithName("CreateBook");

app.MapPut("/books/{id}", (string id, [FromBody] Book updatedBook) =>
{
    if (books.TryGetValue(id, out var existingBook))
    {
        updatedBook.Id = id;
        books[id] = updatedBook;
        return Results.Ok(updatedBook);
    }
    return Results.NotFound();
})
.WithName("UpdateBook");

app.MapDelete("/books/{id}", (string id) =>
{
    return books.TryRemove(id, out _)
        ? Results.NoContent()
        : Results.NotFound();
})
.WithName("DeleteBook");

// Cart endpoints
app.MapGet("/cart/{cartId}", (string cartId) =>
{
    if (!carts.TryGetValue(cartId, out var cart))
    {
        cart = new Cart { Id = cartId };
        carts.TryAdd(cartId, cart);
    }
    return Results.Ok(cart);
})
.WithName("GetCart");

app.MapPost("/cart/{cartId}/items", (string cartId, [FromBody] CartItem item) =>
{
    if (!carts.TryGetValue(cartId, out var cart))
    {
        cart = new Cart { Id = cartId };
        carts.TryAdd(cartId, cart);
    }

    if (!books.TryGetValue(item.BookId, out var book))
    {
        return Results.NotFound("Book not found");
    }

    var existingItem = cart.Items.FirstOrDefault(i => i.BookId == item.BookId);
    if (existingItem != null)
    {
        existingItem.Quantity += item.Quantity;
    }
    else
    {
        item.Book = book;
        cart.Items.Add(item);
    }

    return Results.Ok(cart);
})
.WithName("AddToCart");

app.MapDelete("/cart/{cartId}/items/{bookId}", (string cartId, string bookId) =>
{
    if (carts.TryGetValue(cartId, out var cart))
    {
        var item = cart.Items.FirstOrDefault(i => i.BookId == bookId);
        if (item != null)
        {
            cart.Items.Remove(item);
            return Results.Ok(cart);
        }
    }
    return Results.NotFound();
})
.WithName("RemoveFromCart");

app.MapDelete("/cart/{cartId}", (string cartId) =>
{
    carts.TryRemove(cartId, out _);
    return Results.NoContent();
})
.WithName("ClearCart");

// Order endpoints
app.MapGet("/orders", () =>
{
    return Results.Ok(orders.Values.ToList());
})
.WithName("GetOrders");

app.MapGet("/orders/{id}", (string id) =>
{
    return orders.TryGetValue(id, out var order)
        ? Results.Ok(order)
        : Results.NotFound();
})
.WithName("GetOrder");

app.MapPost("/orders", ([FromBody] Order order) =>
{
    if (string.IsNullOrEmpty(order.Id))
    {
        order.Id = Guid.NewGuid().ToString();
    }
    
    order.OrderDate = DateTime.UtcNow;
    order.TotalAmount = order.Items.Sum(i => i.TotalPrice);
    orders.TryAdd(order.Id, order);
    
    return Results.Created($"/orders/{order.Id}", order);
})
.WithName("CreateOrder");

// Message Queue endpoints (for Worker to consume)
app.MapGet("/queue/messages", () =>
{
    var messages = new List<BookCreatedMessage>();
    while (messageQueue.TryDequeue(out var message))
    {
        messages.Add(message);
    }
    return Results.Ok(messages);
})
.WithName("GetQueueMessages");

// Worker endpoint to update book descriptions
app.MapPut("/books/{id}/description", (string id, [FromBody] string description) =>
{
    if (books.TryGetValue(id, out var book))
    {
        book.Description = description;
        return Results.Ok(book);
    }
    return Results.NotFound();
})
.WithName("UpdateBookDescription");

app.Run();
