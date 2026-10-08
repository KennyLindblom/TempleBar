using PubsApp.Data;

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
    public List<OrderLineResult> Lines { get; set; } = [];
    public decimal Total { get; set; }
}

public class OrderEditViewModel
{
    public int OrderId { get; set; }
    public DateTime? RequiredDate { get; set; }
    public DateTime? ShippedDate { get; set; }
    public int? ShipVia { get; set; }
    public decimal? Freight { get; set; }
    public string? ShipName { get; set; }
    public string? ShipAddress { get; set; }
    public string? ShipCity { get; set; }
    public string? ShipRegion { get; set; }
    public string? ShipPostalCode { get; set; }
    public string? ShipCountry { get; set; }
    public List<Shipper> Shippers { get; set; } = [];
}
