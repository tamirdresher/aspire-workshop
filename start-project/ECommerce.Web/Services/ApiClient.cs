using System.Net.Http.Json;
using ECommerce.Shared.Models;

namespace ECommerce.Web.Services;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiClient> _logger;

    public ApiClient(HttpClient httpClient, IConfiguration configuration, ILogger<ApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // Get API URL from configuration or use default
        var apiUrl = configuration["ApiUrl"] ?? "https://localhost:7001";
        _httpClient.BaseAddress = new Uri(apiUrl);
    }

    public async Task<List<Product>> GetProductsAsync()
    {
        try
        {
            var products = await _httpClient.GetFromJsonAsync<List<Product>>("api/products");
            return products ?? new List<Product>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching products");
            return new List<Product>();
        }
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<Product>($"api/products/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching product {ProductId}", id);
            return null;
        }
    }

    public async Task<List<Order>> GetOrdersAsync()
    {
        try
        {
            var orders = await _httpClient.GetFromJsonAsync<List<Order>>("api/orders");
            return orders ?? new List<Order>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching orders");
            return new List<Order>();
        }
    }

    public async Task<Order?> CreateOrderAsync(Order order)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/orders", order);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Order>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return null;
        }
    }
}
