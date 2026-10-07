using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PubsApp.Data;
using PubsApp.Models;

namespace PubsApp.Controllers;

public class ProductsController(NorthwindContext database) : Controller
{
    public async Task<IActionResult> Index(string? search, int? categoryId, string stock = "all", int page = 1)
    {
        const int pageSize = 20;
        var products = database.Products.AsNoTracking().Include(x => x.Category).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            products = products.Where(x => EF.Functions.Like(x.ProductName, $"%{search.Trim()}%"));
        if (categoryId.HasValue)
            products = products.Where(x => x.CategoryId == categoryId.Value);
        stock = stock is "low" or "out" or "discontinued" ? stock : "all";
        if (stock == "low")
            products = products.Where(x => !x.Discontinued && x.UnitsInStock <= x.ReorderLevel && x.UnitsInStock > 0);
        else if (stock == "out")
            products = products.Where(x => !x.Discontinued && x.UnitsInStock == 0);
        else if (stock == "discontinued")
            products = products.Where(x => x.Discontinued);

        var count = await products.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(count / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);

        return View(new ProductListViewModel
        {
            Search = search,
            CategoryId = categoryId,
            Stock = stock,
            Page = page,
            TotalPages = totalPages,
            TotalCount = count,
            Categories = await database.Categories.AsNoTracking().OrderBy(x => x.CategoryName).ToListAsync(),
            Products = await products.OrderBy(x => x.ProductName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync()
        });
    }
}
