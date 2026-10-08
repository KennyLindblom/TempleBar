namespace PubsApp.Models;

public class ProductListViewModel
{
    public string? Search { get; set; }
    public string? Message { get; set; }
    public int? CategoryId { get; set; }
    public string Stock { get; set; } = "all";
    public List<Product> Products { get; set; } = [];
    public List<Category> Categories { get; set; } = [];
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
}

public class ProductDetailsViewModel
{
    public Product Product { get; set; } = new();
    public int OrderLineCount { get; set; }
    public int UnitsSold { get; set; }
}

public class ProductEditViewModel
{
    public Product Product { get; set; } = new();
    public List<Category> Categories { get; set; } = [];
}

public class ProductDeleteViewModel
{
    public Product Product { get; set; } = new();
    public bool HasOrderHistory { get; set; }
}
