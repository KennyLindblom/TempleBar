namespace PubsApp.Models;

public class DashboardViewModel
{
    public int CustomerCount { get; set; }
    public int OrderCount { get; set; }
    public int ProductCount { get; set; }
    public int LowStockCount { get; set; }
    public decimal GrossSales { get; set; }
    public decimal AverageOrderValue { get; set; }
    public List<RecentOrderViewModel> RecentOrders { get; set; } = [];
    public List<Product> LowStockProducts { get; set; } = [];
    public List<MonthlySalesViewModel> MonthlySales { get; set; } = [];
    public List<CategorySalesViewModel> TopCategories { get; set; } = [];
    public string SalesPeriod { get; set; } = "";
}

public class RecentOrderViewModel
{
    public int OrderId { get; set; }
    public string CustomerName { get; set; } = "";
    public DateTime? OrderDate { get; set; }
    public DateTime? ShippedDate { get; set; }
    public decimal Total { get; set; }
}

public class MonthlySalesViewModel
{
    public string Label { get; set; } = "";
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
}

public class CategorySalesViewModel
{
    public string Name { get; set; } = "";
    public decimal Revenue { get; set; }
}
