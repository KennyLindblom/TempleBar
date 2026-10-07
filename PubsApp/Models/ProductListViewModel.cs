namespace PubsApp.Models;

public class ProductListViewModel
{
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public string Stock { get; set; } = "all";
    public List<Product> Products { get; set; } = [];
    public List<Category> Categories { get; set; } = [];
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
}
