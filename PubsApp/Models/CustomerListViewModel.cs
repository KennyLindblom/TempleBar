using PubsApp.Data;

namespace PubsApp.Models;

public class CustomerListViewModel
{
    public string? Search { get; set; }
    public string? Message { get; set; }
    public List<Customer> Customers { get; set; } = [];
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
}

public class CustomerDetailsViewModel
{
    public Customer Customer { get; set; } = new();
    public List<CustomerOrderResult> Orders { get; set; } = [];
    public List<CustomerProductHistoryResult> ProductHistory { get; set; } = [];
    public bool HasOrders { get; set; }
}

public class CustomerDeleteViewModel
{
    public Customer Customer { get; set; } = new();
    public bool HasOrders { get; set; }
}
