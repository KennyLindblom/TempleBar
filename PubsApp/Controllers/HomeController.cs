using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using PubsApp.Data;
using PubsApp.Models;

namespace PubsApp.Controllers;

public class HomeController(NorthwindContext database) : Controller
{
    public async Task<IActionResult> Index()
    {
        var lines = await database.OrderDetails.AsNoTracking()
            .Include(x => x.Order)
            .Include(x => x.Product).ThenInclude(x => x!.Category)
            .ToListAsync();
        var salesLines = lines.Select(line => new
        {
            line.OrderId,
            Date = line.Order?.OrderDate,
            Revenue = line.UnitPrice * line.Quantity * (1m - Convert.ToDecimal(line.Discount)),
            Category = line.Product?.Category?.CategoryName ?? "Uncategorized"
        }).ToList();

        var lastSaleDate = salesLines.Where(x => x.Date.HasValue)
            .Select(x => x.Date!.Value).DefaultIfEmpty(DateTime.Today).Max();
        var lastMonth = new DateTime(lastSaleDate.Year, lastSaleDate.Month, 1);
        var firstMonth = lastMonth.AddMonths(-11);
        var monthlySales = Enumerable.Range(0, 12).Select(offset =>
        {
            var month = firstMonth.AddMonths(offset);
            var monthLines = salesLines.Where(x => x.Date.HasValue &&
                x.Date.Value.Year == month.Year && x.Date.Value.Month == month.Month).ToList();
            return new MonthlySalesViewModel
            {
                Label = month.ToString("MMM yy"),
                Revenue = monthLines.Sum(x => x.Revenue),
                OrderCount = monthLines.Select(x => x.OrderId).Distinct().Count()
            };
        }).ToList();

        var orders = await database.Orders.AsNoTracking().Include(x => x.Customer)
            .OrderByDescending(x => x.OrderDate).Take(10).ToListAsync();
        var totalsByOrder = salesLines.GroupBy(x => x.OrderId)
            .ToDictionary(x => x.Key, x => x.Sum(line => line.Revenue));
        var grossSales = salesLines.Sum(x => x.Revenue);
        var orderCount = await database.Orders.CountAsync();
        var model = new DashboardViewModel
        {
            CustomerCount = await database.Customers.CountAsync(),
            OrderCount = orderCount,
            ProductCount = await database.Products.CountAsync(),
            LowStockCount = await database.Products.CountAsync(x => !x.Discontinued && x.UnitsInStock <= x.ReorderLevel),
            GrossSales = grossSales,
            AverageOrderValue = orderCount == 0 ? 0 : grossSales / orderCount,
            MonthlySales = monthlySales,
            SalesPeriod = $"{firstMonth:MMM yyyy} – {lastMonth:MMM yyyy}",
            TopCategories = salesLines.GroupBy(x => x.Category)
                .Select(x => new CategorySalesViewModel { Name = x.Key, Revenue = x.Sum(line => line.Revenue) })
                .OrderByDescending(x => x.Revenue).Take(5).ToList(),
            RecentOrders = orders.Select(x => new RecentOrderViewModel
            {
                OrderId = x.OrderId,
                CustomerName = x.Customer?.CompanyName ?? "(No customer)",
                OrderDate = x.OrderDate,
                ShippedDate = x.ShippedDate,
                Total = totalsByOrder.GetValueOrDefault(x.OrderId) + (x.Freight ?? 0)
            }).ToList(),
            LowStockProducts = await database.Products.Include(x => x.Category)
                .Where(x => !x.Discontinued && x.UnitsInStock <= x.ReorderLevel)
                .OrderBy(x => x.UnitsInStock).Take(8).ToListAsync()
        };
        return View(model);
    }

    public IActionResult Error() => View(new ErrorViewModel
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
    });
}
