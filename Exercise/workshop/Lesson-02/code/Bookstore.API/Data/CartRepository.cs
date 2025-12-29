using Microsoft.EntityFrameworkCore;
using Bookstore.Shared;

namespace Bookstore.API.Data;

public class CartRepository
{
    private readonly BookstoreDbContext _context;
    private readonly BookstoreRepository _bookRepository;

    public CartRepository(BookstoreDbContext context, BookstoreRepository bookRepository)
    {
        _context = context;
        _bookRepository = bookRepository;
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
            var book = await _bookRepository.GetBookAsync(item.BookId);
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
