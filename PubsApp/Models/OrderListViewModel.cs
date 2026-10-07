namespace PubsApp.Models;

public class OrderListViewModel
{
    public string? Search { get; set; }
    public string Status { get; set; } = "all";
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public List<RecentOrderViewModel> Orders { get; set; } = [];
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
}

public class OrderDetailsViewModel
{
    public Order Order { get; set; } = new();
    public decimal Total { get; set; }
}
