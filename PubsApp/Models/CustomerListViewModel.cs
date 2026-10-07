namespace PubsApp.Models;

public class CustomerListViewModel
{
    public string? Search { get; set; }
    public List<Customer> Customers { get; set; } = [];
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
}
