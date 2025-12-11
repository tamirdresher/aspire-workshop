using ECommerce.Shared.Models;
using System.Text.Json;

namespace Basket.API.Services;

/// <summary>
/// Client for calling the Catalog API to validate products.
/// This demonstrates the challenge of manual service discovery - we need to hardcode the URL.
/// </summary>
public class CatalogClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CatalogClient> _logger;
    private readonly string _catalogApiUrl;

    public CatalogClient(HttpClient httpClient, IConfiguration configuration, ILogger<CatalogClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // PROBLEM: Hardcoded URL from configuration
        // This needs to be updated for every environment (dev, staging, production)
        // If Catalog.API changes ports, this breaks
        _catalogApiUrl = configuration["ServiceUrls:CatalogApi"] 
            ?? throw new InvalidOperationException("CatalogApi URL not configured in appsettings.json");
        
        _httpClient.BaseAddress = new Uri(_catalogApiUrl);
        
        _logger.LogInformation("CatalogClient configured with base URL: {CatalogApiUrl}", _catalogApiUrl);
    }

    /// <summary>
    /// Validates that a product exists in the catalog
    /// </summary>
    public async Task<bool> ValidateProductExistsAsync(string productId)
    {
        try
        {
            _logger.LogInformation("Validating product {ProductId} exists in Catalog API at {Url}", 
                productId, _catalogApiUrl);
            
            var response = await _httpClient.GetAsync($"/api/catalog/{productId}");
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Product {ProductId} validated successfully", productId);
                return true;
            }
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Product {ProductId} not found in catalog", productId);
                return false;
            }
            
            _logger.LogError("Unexpected response from Catalog API: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (HttpRequestException ex)
        {
            // PROBLEM: If Catalog.API is not running, we get this exception
            // In production, this could mean the service is down or at a different URL
            _logger.LogError(ex, 
                "Failed to connect to Catalog API at {Url}. Is the service running on the correct port?", 
                _catalogApiUrl);
            throw new InvalidOperationException(
                $"Cannot validate product - Catalog API unavailable at {_catalogApiUrl}. " +
                "Ensure Catalog.API is running on port 7001.", ex);
        }
    }

    /// <summary>
    /// Gets product details from the catalog
    /// </summary>
    public async Task<CatalogItem?> GetProductAsync(string productId)
    {
        try
        {
            _logger.LogInformation("Fetching product {ProductId} from Catalog API", productId);
            
            var response = await _httpClient.GetAsync($"/api/catalog/{productId}");
            
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Product {ProductId} not found", productId);
                    return null;
                }
                
                _logger.LogError("Failed to fetch product {ProductId}: {StatusCode}", 
                    productId, response.StatusCode);
                return null;
            }
            
            var content = await response.Content.ReadAsStringAsync();
            var product = JsonSerializer.Deserialize<CatalogItem>(content, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            return product;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to connect to Catalog API at {Url}", _catalogApiUrl);
            throw new InvalidOperationException(
                $"Cannot fetch product - Catalog API unavailable at {_catalogApiUrl}", ex);
        }
    }
}