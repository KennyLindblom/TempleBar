using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PubsApp.Data;
using PubsApp.Models;

namespace PubsApp.Controllers;

public class CustomersController(NorthwindContext database) : Controller
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
            Page = page,
            TotalPages = totalPages,
            TotalCount = count,
            Customers = await customers.OrderBy(x => x.CompanyName)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync()
        });
    }
}
