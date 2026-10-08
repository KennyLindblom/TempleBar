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

public class OpenFoodFactsSearchViewModel
{
    public string? Query { get; set; }
    public string? Message { get; set; }
    public List<PubsApp.Data.OpenFoodFactsProduct> Results { get; set; } = [];
}

public class ExternalProductImportViewModel
{
    public string Barcode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string? Quantity { get; set; }
    public string? SourceCategory { get; set; }
    public string? ImageUrl { get; set; }
    public int? CategoryId { get; set; }
    public decimal? UnitPrice { get; set; }
    public List<Category> Categories { get; set; } = [];
}
