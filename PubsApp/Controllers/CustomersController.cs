using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PubsApp.Data;
using PubsApp.Models;

namespace PubsApp.Controllers;

public class CustomersController(NorthwindContext database, NorthwindProcedures procedures) : Controller
{
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        const int pageSize = 20;
        var customers = database.Customers.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            customers = customers.Where(x => EF.Functions.Like(x.CompanyName, $"%{term}%") ||
                (x.ContactName != null && EF.Functions.Like(x.ContactName, $"%{term}%")) ||
                (x.City != null && EF.Functions.Like(x.City, $"%{term}%")) ||
                (x.Country != null && EF.Functions.Like(x.Country, $"%{term}%")));
        }

        var count = await customers.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(count / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);
        return View(new CustomerListViewModel
        {
            Search = search,
            Message = TempData["Message"] as string,
            Page = page,
            TotalPages = totalPages,
            TotalCount = count,
            Customers = await customers.OrderBy(x => x.CompanyName)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync()
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(string id)
    {
        var customer = await database.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.CustomerId == id);
        if (customer is null)
            return NotFound();

        var orders = await procedures.GetCustomerOrdersAsync(customer.CustomerId);
        var productHistory = await procedures.GetCustomerProductHistoryAsync(customer.CustomerId);
        return View(new CustomerDetailsViewModel
        {
            Customer = customer,
            Orders = orders,
            ProductHistory = productHistory,
            HasOrders = orders.Count > 0
        });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var customer = await database.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.CustomerId == id);
        return customer is null ? NotFound() : View(customer);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, [Bind("CustomerId,CompanyName,ContactName,ContactTitle,Address,City,Region,PostalCode,Country,Phone,Fax", Prefix = "")] Customer input)
    {
        if (!string.Equals(id, input.CustomerId, StringComparison.OrdinalIgnoreCase))
            return BadRequest();
        if (string.IsNullOrWhiteSpace(input.CompanyName))
            ModelState.AddModelError(nameof(input.CompanyName), "Company name is required.");
        if (input.CustomerId.Length != 5 || input.CompanyName.Length > 40 || input.ContactName?.Length > 30 ||
            input.ContactTitle?.Length > 30 || input.Address?.Length > 60 || input.City?.Length > 15 ||
            input.Region?.Length > 15 || input.PostalCode?.Length > 10 || input.Country?.Length > 15 ||
            input.Phone?.Length > 24 || input.Fax?.Length > 24)
            ModelState.AddModelError(string.Empty, "One or more values exceed the database field length.");
        if (!ModelState.IsValid)
            return View(input);

        var customer = await database.Customers.FirstOrDefaultAsync(x => x.CustomerId == id);
        if (customer is null)
            return NotFound();

        customer.CompanyName = input.CompanyName.Trim();
        customer.ContactName = Clean(input.ContactName);
        customer.ContactTitle = Clean(input.ContactTitle);
        customer.Address = Clean(input.Address);
        customer.City = Clean(input.City);
        customer.Region = Clean(input.Region);
        customer.PostalCode = Clean(input.PostalCode);
        customer.Country = Clean(input.Country);
        customer.Phone = Clean(input.Phone);
        customer.Fax = Clean(input.Fax);
        await database.SaveChangesAsync();
        TempData["Message"] = "Customer updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Delete(string id)
    {
        var customer = await database.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.CustomerId == id);
        if (customer is null)
            return NotFound();
        return View(new CustomerDeleteViewModel
        {
            Customer = customer,
            HasOrders = await database.Orders.AnyAsync(x => x.CustomerId == id)
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var customer = await database.Customers.FirstOrDefaultAsync(x => x.CustomerId == id);
        if (customer is null)
            return NotFound();
        if (await database.Orders.AnyAsync(x => x.CustomerId == id))
        {
            TempData["Message"] = "This customer has order history and cannot be deleted. You can edit the customer record instead.";
            return RedirectToAction(nameof(Details), new { id });
        }

        database.Customers.Remove(customer);
        await database.SaveChangesAsync();
        TempData["Message"] = "Customer deleted.";
        return RedirectToAction(nameof(Index));
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
