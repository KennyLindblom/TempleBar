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
            Message = TempData["Message"] as string,
            CategoryId = categoryId,
            Stock = stock,
            Page = page,
            TotalPages = totalPages,
            TotalCount = count,
            Categories = await database.Categories.AsNoTracking().OrderBy(x => x.CategoryName).ToListAsync(),
            Products = await products.OrderBy(x => x.ProductName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync()
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var product = await database.Products.AsNoTracking().Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.ProductId == id);
        if (product is null)
            return NotFound();
        var lines = database.OrderDetails.Where(x => x.ProductId == id);
        return View(new ProductDetailsViewModel
        {
            Product = product,
            OrderLineCount = await lines.CountAsync(),
            UnitsSold = await lines.SumAsync(x => (int)x.Quantity)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await database.Products.AsNoTracking().FirstOrDefaultAsync(x => x.ProductId == id);
        if (product is null)
            return NotFound();
        return View(await BuildEditModel(product));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductEditViewModel model)
    {
        var input = model.Product;
        if (id != input.ProductId)
            return BadRequest();
        ValidateProduct(input);
        if (!ModelState.IsValid)
            return View(await BuildEditModel(input));

        var product = await database.Products.FirstOrDefaultAsync(x => x.ProductId == id);
        if (product is null)
            return NotFound();
        product.ProductName = input.ProductName.Trim();
        product.CategoryId = input.CategoryId;
        product.QuantityPerUnit = Clean(input.QuantityPerUnit);
        product.UnitPrice = input.UnitPrice;
        product.UnitsInStock = input.UnitsInStock;
        product.UnitsOnOrder = input.UnitsOnOrder;
        product.ReorderLevel = input.ReorderLevel;
        product.Discontinued = input.Discontinued;
        await database.SaveChangesAsync();
        TempData["Message"] = "Product updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await database.Products.AsNoTracking().FirstOrDefaultAsync(x => x.ProductId == id);
        if (product is null)
            return NotFound();
        return View(new ProductDeleteViewModel
        {
            Product = product,
            HasOrderHistory = await database.OrderDetails.AnyAsync(x => x.ProductId == id)
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var product = await database.Products.FirstOrDefaultAsync(x => x.ProductId == id);
        if (product is null)
            return NotFound();
        if (await database.OrderDetails.AnyAsync(x => x.ProductId == id))
        {
            TempData["Message"] = "This product appears in order history and cannot be deleted. Mark it discontinued instead.";
            return RedirectToAction(nameof(Details), new { id });
        }
        database.Products.Remove(product);
        await database.SaveChangesAsync();
        TempData["Message"] = "Product deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<ProductEditViewModel> BuildEditModel(Product product) => new()
    {
        Product = product,
        Categories = await database.Categories.AsNoTracking().OrderBy(x => x.CategoryName).ToListAsync()
    };

    private void ValidateProduct(Product product)
    {
        if (string.IsNullOrWhiteSpace(product.ProductName) || product.ProductName.Trim().Length > 40)
            ModelState.AddModelError("Product.ProductName", "Enter a product name up to 40 characters.");
        if (product.QuantityPerUnit?.Length > 20)
            ModelState.AddModelError("Product.QuantityPerUnit", "Package description must be 20 characters or fewer.");
        if (product.UnitPrice < 0)
            ModelState.AddModelError("Product.UnitPrice", "Price cannot be negative.");
        if (product.UnitsInStock < 0 || product.UnitsOnOrder < 0 || product.ReorderLevel < 0)
        {
            ModelState.AddModelError("Product.UnitsInStock", "Inventory values cannot be negative.");
            ModelState.AddModelError("Product.UnitsOnOrder", "Inventory values cannot be negative.");
            ModelState.AddModelError("Product.ReorderLevel", "Inventory values cannot be negative.");
        }
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
