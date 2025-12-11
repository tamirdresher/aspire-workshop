using ECommerce.Shared.Models;
using System.Text.Json;

namespace Ordering.API.Services;

/// <summary>
/// Client for calling the Basket API to retrieve basket data during checkout.
/// This demonstrates the complexity of managing service-to-service communication manually.
/// </summary>
public class BasketClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BasketClient> _logger;
    private readonly string _basketApiUrl;

    public BasketClient(HttpClient httpClient, IConfiguration configuration, ILogger<BasketClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // PROBLEM: Another hardcoded URL to manage
        // Every service that calls another service needs its own configuration
        // This multiplies quickly: 4 services × 3 environments = 12 URL configurations to maintain
        _basketApiUrl = configuration["ServiceUrls:BasketApi"] 
            ?? throw new InvalidOperationException("BasketApi URL not configured in appsettings.json");
        
        _httpClient.BaseAddress = new Uri(_basketApiUrl);
        
        _logger.LogInformation("BasketClient configured with base URL: {BasketApiUrl}", _basketApiUrl);
    }

    /// <summary>
    /// Retrieves a customer's basket from the Basket API
    /// </summary>
    public async Task<CustomerBasket?> GetBasketAsync(string buyerId)
    {
        try
        {
            _logger.LogInformation("Fetching basket for buyer {BuyerId} from Basket API at {Url}", 
                buyerId, _basketApiUrl);
            
            var response = await _httpClient.GetAsync($"/api/basket/{buyerId}");
            
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Basket not found for buyer {BuyerId}", buyerId);
                    return null;
                }
                
                _logger.LogError("Failed to fetch basket for {BuyerId}: {StatusCode}", 
                    buyerId, response.StatusCode);
                return null;
            }
            
            var content = await response.Content.ReadAsStringAsync();
            var basket = JsonSerializer.Deserialize<CustomerBasket>(content, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            _logger.LogInformation("Successfully retrieved basket for {BuyerId} with {ItemCount} items", 
                buyerId, basket?.Items.Count ?? 0);
            
            return basket;
        }
        catch (HttpRequestException ex)
        {
            // PROBLEM: Service dependency failure
            // If Basket.API is down or URL is wrong, the entire ordering flow breaks
            // No automatic retry, no circuit breaker, no service discovery
            _logger.LogError(ex, 
                "Failed to connect to Basket API at {Url}. Is the service running on the correct port?", 
                _basketApiUrl);
            throw new InvalidOperationException(
                $"Cannot retrieve basket - Basket API unavailable at {_basketApiUrl}. " +
                "Ensure Basket.API is running on port 7002.", ex);
        }
    }

    /// <summary>
    /// Deletes a basket after successful order creation
    /// </summary>
    public async Task<bool> DeleteBasketAsync(string buyerId)
    {
        try
        {
            _logger.LogInformation("Deleting basket for buyer {BuyerId}", buyerId);
            
            var response = await _httpClient.DeleteAsync($"/api/basket/{buyerId}");
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully deleted basket for {BuyerId}", buyerId);
                return true;
            }
            
            _logger.LogWarning("Failed to delete basket for {BuyerId}: {StatusCode}", 
                buyerId, response.StatusCode);
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to delete basket for {BuyerId}", buyerId);
            // Don't throw - basket deletion is not critical for order creation
            return false;
        }
    }
}