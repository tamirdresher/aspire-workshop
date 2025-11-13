using ECommerce.Shared.Models;
using ECommerce.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ECommerce.Web.Pages;

public class ProductsModel : PageModel
{
    private readonly ApiClient _apiClient;
    private readonly IConfiguration _configuration;

    public ProductsModel(ApiClient apiClient, IConfiguration configuration)
    {
        _apiClient = apiClient;
        _configuration = configuration;
    }

    public List<Product> Products { get; set; } = new();
    public string ApiUrl => _configuration["ApiUrl"] ?? "https://localhost:7001";

    public async Task OnGetAsync()
    {
        Products = await _apiClient.GetProductsAsync();
    }
}
