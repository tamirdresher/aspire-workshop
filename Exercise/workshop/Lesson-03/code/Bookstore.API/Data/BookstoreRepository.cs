using Microsoft.EntityFrameworkCore;
using Bookstore.Shared;

namespace Bookstore.API.Data;

public class BookstoreRepository
{
    private readonly BookstoreDbContext _context;

    public BookstoreRepository(BookstoreDbContext context)
    {
        _context = context;
    }

    public async Task<List<Book>> GetBooksAsync()
    {
        return await _context.Books.ToListAsync();
    }

    public async Task<Book?> GetBookAsync(string id)
    {
        return await _context.Books.FindAsync(id);
    }

    public async Task CreateBookAsync(Book book)
    {
        _context.Books.Add(book);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateBookAsync(string id, Book book)
    {
        var existingBook = await _context.Books.FindAsync(id);
        if (existingBook == null)
        {
            return;
        }

        existingBook.Title = book.Title;
        existingBook.Author = book.Author;
        existingBook.Price = book.Price;
        existingBook.ImageUrl = book.ImageUrl;
        existingBook.Description = book.Description;
        existingBook.Stock = book.Stock;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteBookAsync(string id)
    {
        var book = await _context.Books.FindAsync(id);
        if (book != null)
        {
            _context.Books.Remove(book);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<Cart> GetCartAsync(string cartId)
    {
        var cart = await _context.Carts.FindAsync(cartId);
        if (cart == null)
        {
            cart = new Cart { Id = cartId };
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();
        }
        return cart;
    }

    public async Task<Cart> AddToCartAsync(string cartId, CartItem item)
    {
        var cart = await GetCartAsync(cartId);
        var existingItem = cart.Items.FirstOrDefault(i => i.BookId == item.BookId);
        if (existingItem != null)
        {
            existingItem.Quantity += item.Quantity;
        }
        else
        {
            var book = await GetBookAsync(item.BookId);
            if (book != null)
            {
                item.Book = book;
                cart.Items.Add(item);
            }
        }

        await _context.SaveChangesAsync();
        return cart;
    }

    public async Task<Cart> RemoveFromCartAsync(string cartId, string bookId)
    {
        var cart = await GetCartAsync(cartId);
        var item = cart.Items.FirstOrDefault(i => i.BookId == bookId);
        if (item != null)
        {
            cart.Items.Remove(item);
            await _context.SaveChangesAsync();
        }
        return cart;
    }

    public async Task ClearCartAsync(string cartId)
    {
        var cart = await _context.Carts.FindAsync(cartId);
        if (cart != null)
        {
            _context.Carts.Remove(cart);
            await _context.SaveChangesAsync();
        }
    }
}
