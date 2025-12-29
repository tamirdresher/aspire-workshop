using Bookstore.Shared;

namespace Bookstore.Web.Clients;

public class BookstoreClient(HttpClient client)
{
    public async Task<Book[]> GetBooksAsync()
    {
        return await client.GetFromJsonAsync<Book[]>("/books") ?? [];
    }

    public async Task<Cart?> GetCartAsync(string cartId)
    {
        return await client.GetFromJsonAsync<Cart>($"/cart/{cartId}");
    }

    public async Task<Cart?> AddToCartAsync(string cartId, CartItem item)
    {
        var response = await client.PostAsJsonAsync($"/cart/{cartId}/items", item);
        return await response.Content.ReadFromJsonAsync<Cart>();
    }

    public async Task<Cart?> RemoveFromCartAsync(string cartId, string bookId)
    {
        var response = await client.DeleteAsync($"/cart/{cartId}/items/{bookId}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Cart>();
        }
        return null;
    }

    public async Task ClearCartAsync(string cartId)
    {
        await client.DeleteAsync($"/cart/{cartId}");
    }

    public async Task<Order?> CreateOrderAsync(Order order)
    {
        var response = await client.PostAsJsonAsync("/orders", order);
        return await response.Content.ReadFromJsonAsync<Order>();
    }
}
