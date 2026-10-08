using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PubsApp.Data;
using PubsApp.Models;

namespace PubsApp.Controllers;

public class OrdersController(NorthwindContext database, NorthwindProcedures procedures) : Controller
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
            .FirstOrDefaultAsync(x => x.OrderId == id);
        if (order is null)
            return NotFound();

        var lines = await procedures.GetOrderDetailsAsync(id);
        var total = lines.Sum(x => x.ExtendedPrice);
        return View(new OrderDetailsViewModel { Order = order, Lines = lines, Total = total });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var order = await database.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.OrderId == id);
        if (order is null) return NotFound();
        return View(await ToEditModel(order));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, OrderEditViewModel model)
    {
        if (id != model.OrderId) return BadRequest();
        if (model.Freight < 0) ModelState.AddModelError(nameof(model.Freight), "Freight cannot be negative.");
        if (model.RequiredDate.HasValue && model.ShippedDate.HasValue && model.ShippedDate < model.RequiredDate)
            ModelState.AddModelError(nameof(model.ShippedDate), "Shipped date is earlier than the required date.");
        if (!ModelState.IsValid) return View(await ToEditModel(model));
        var order = await database.Orders.FirstOrDefaultAsync(x => x.OrderId == id);
        if (order is null) return NotFound();
        order.RequiredDate = model.RequiredDate; order.ShippedDate = model.ShippedDate;
        order.ShipVia = model.ShipVia; order.Freight = model.Freight;
        order.ShipName = model.ShipName; order.ShipAddress = model.ShipAddress; order.ShipCity = model.ShipCity;
        order.ShipRegion = model.ShipRegion; order.ShipPostalCode = model.ShipPostalCode; order.ShipCountry = model.ShipCountry;
        await database.SaveChangesAsync();
        TempData["Message"] = $"Order #{id} updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var order = await database.Orders.AsNoTracking().Include(x => x.Customer).FirstOrDefaultAsync(x => x.OrderId == id);
        return order is null ? NotFound() : View(order);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await using var transaction = await database.Database.BeginTransactionAsync();
        var order = await database.Orders.FirstOrDefaultAsync(x => x.OrderId == id);
        if (order is null) return NotFound();
        var lines = await database.OrderDetails.Where(x => x.OrderId == id).ToListAsync();
        database.OrderDetails.RemoveRange(lines);
        database.Orders.Remove(order);
        await database.SaveChangesAsync();
        await transaction.CommitAsync();
        TempData["Message"] = $"Order #{id} deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<OrderEditViewModel> ToEditModel(Order order) => new()
    {
        OrderId = order.OrderId, RequiredDate = order.RequiredDate, ShippedDate = order.ShippedDate,
        ShipVia = order.ShipVia, Freight = order.Freight, ShipName = order.ShipName, ShipAddress = order.ShipAddress,
        ShipCity = order.ShipCity, ShipRegion = order.ShipRegion, ShipPostalCode = order.ShipPostalCode,
        ShipCountry = order.ShipCountry, Shippers = await database.Shippers.AsNoTracking().OrderBy(x => x.CompanyName).ToListAsync()
    };

    private async Task<OrderEditViewModel> ToEditModel(OrderEditViewModel model)
    {
        model.Shippers = await database.Shippers.AsNoTracking().OrderBy(x => x.CompanyName).ToListAsync();
        return model;
    }
}
