using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PubsApp.Data;
using PubsApp.Models;

namespace PubsApp.Controllers;

public class OrdersController(NorthwindContext database) : Controller
{
    public async Task<IActionResult> Index(string? search, string status = "all", DateTime? dateFrom = null, DateTime? dateTo = null, int page = 1)
    {
        const int pageSize = 25;
        var orders = database.Orders.AsNoTracking().Include(x => x.Customer).Include(x => x.OrderDetails).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            if (int.TryParse(term, out var orderId))
                orders = orders.Where(x => x.OrderId == orderId ||
                    (x.Customer != null && EF.Functions.Like(x.Customer.CompanyName, $"%{term}%")));
            else
                orders = orders.Where(x => x.Customer != null && EF.Functions.Like(x.Customer.CompanyName, $"%{term}%"));
        }
        status = status is "open" or "shipped" ? status : "all";
        if (status == "open")
            orders = orders.Where(x => x.ShippedDate == null);
        else if (status == "shipped")
            orders = orders.Where(x => x.ShippedDate != null);
        if (dateFrom.HasValue)
            orders = orders.Where(x => x.OrderDate >= dateFrom.Value.Date);
        if (dateTo.HasValue)
            orders = orders.Where(x => x.OrderDate < dateTo.Value.Date.AddDays(1));

        var count = await orders.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(count / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);
        var rows = await orders.OrderByDescending(x => x.OrderDate).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return View(new OrderListViewModel
        {
            Search = search,
            Status = status,
            DateFrom = dateFrom,
            DateTo = dateTo,
            Page = page,
            TotalPages = totalPages,
            TotalCount = count,
            Orders = rows.Select(x => new RecentOrderViewModel
            {
                OrderId = x.OrderId,
                CustomerName = x.Customer?.CompanyName ?? "(No customer)",
                OrderDate = x.OrderDate,
                ShippedDate = x.ShippedDate,
                Total = x.OrderDetails.Sum(d => d.UnitPrice * d.Quantity * (1m - Convert.ToDecimal(d.Discount))) + (x.Freight ?? 0)
            }).ToList()
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await database.Orders.AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Employee)
            .Include(x => x.Shipper)
            .Include(x => x.OrderDetails).ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.OrderId == id);
        if (order is null)
            return NotFound();

        var total = order.OrderDetails.Sum(x => x.UnitPrice * x.Quantity * (1m - Convert.ToDecimal(x.Discount)));
        return View(new OrderDetailsViewModel { Order = order, Total = total });
    }
}
