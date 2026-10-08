using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PubsApp.Data;
using PubsApp.Models;

namespace PubsApp.Controllers;

public class ProductsController(NorthwindContext database, OpenFoodFactsClient openFoodFacts) : Controller
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
    public async Task<IActionResult> ImportSearch(string? query, CancellationToken cancellationToken)
    {
        var model = new OpenFoodFactsSearchViewModel { Query = query, Message = TempData["Message"] as string };
        if (string.IsNullOrWhiteSpace(query)) return View(model);
        query = query.Trim();
        if (query.Length < 2 || query.Length > 80)
        {
            model.Message = "Enter between 2 and 80 characters to search.";
            return View(model);
        }

        try
        {
            var result = await openFoodFacts.SearchAsync(query, cancellationToken);
            model.Results = result.Products.Where(p => !string.IsNullOrWhiteSpace(p.Code) &&
                !string.IsNullOrWhiteSpace(p.ProductName)).ToList();
            if (model.Results.Count == 0) model.Message = "No products found. Try another search.";
        }
        catch (HttpRequestException)
        {
            model.Message = "Open Food Facts could not be reached right now. Please try again shortly.";
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ImportReview(string barcode, CancellationToken cancellationToken)
    {
        if (!IsValidBarcode(barcode)) return BadRequest();
        try
        {
            var external = await openFoodFacts.GetProductAsync(barcode, cancellationToken);
            if (external is null || string.IsNullOrWhiteSpace(external.ProductName)) return NotFound();
            return View(await BuildImportModel(external));
        }
        catch (HttpRequestException)
        {
            TempData["Message"] = "Open Food Facts could not be reached right now. Please try again shortly.";
            return RedirectToAction(nameof(ImportSearch));
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(ExternalProductImportViewModel model, CancellationToken cancellationToken)
    {
        if (!IsValidBarcode(model.Barcode)) return BadRequest();
        if (string.IsNullOrWhiteSpace(model.ProductName) || model.ProductName.Trim().Length > 40)
            ModelState.AddModelError(nameof(model.ProductName), "Product name is required and must be 40 characters or fewer.");
        if (model.Quantity?.Length > 20)
            ModelState.AddModelError(nameof(model.Quantity), "Package description must be 20 characters or fewer.");
        if (model.UnitPrice < 0)
            ModelState.AddModelError(nameof(model.UnitPrice), "Price cannot be negative.");
        if (model.CategoryId.HasValue && !await database.Categories.AnyAsync(x => x.CategoryId == model.CategoryId, cancellationToken))
            ModelState.AddModelError(nameof(model.CategoryId), "Choose a valid category.");

        OpenFoodFactsProduct? external;
        try { external = await openFoodFacts.GetProductAsync(model.Barcode, cancellationToken); }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "Open Food Facts could not be reached. Please try again shortly.");
            external = null;
        }
        if (external is null)
            ModelState.AddModelError(string.Empty, "That product could not be verified with Open Food Facts.");

        if (ModelState.IsValid)
        {
            var productName = model.ProductName.Trim();
            if (await database.Products.AnyAsync(x => x.ProductName == productName, cancellationToken))
                ModelState.AddModelError(nameof(model.ProductName), "A product with this name already exists in your catalog.");
            else
            {
                database.Products.Add(new Product
                {
                    ProductName = productName,
                    CategoryId = model.CategoryId,
                    QuantityPerUnit = string.IsNullOrWhiteSpace(model.Quantity) ? null : model.Quantity.Trim(),
                    UnitPrice = model.UnitPrice,
                    UnitsInStock = 0,
                    UnitsOnOrder = 0,
                    ReorderLevel = 0,
                    Discontinued = false
                });
                await database.SaveChangesAsync(cancellationToken);
                TempData["Message"] = $"{productName} was added to your product catalog. Set its inventory from the product edit page when ready.";
                return RedirectToAction(nameof(Index));
            }
        }

        model.Categories = await GetCategories();
        model.SourceCategory = external?.Categories ?? model.SourceCategory;
        model.ImageUrl = external?.ImageUrl ?? model.ImageUrl;
        return View("ImportReview", model);
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

    private async Task<ExternalProductImportViewModel> BuildImportModel(OpenFoodFactsProduct external)
    {
        var categories = await GetCategories();
        var beveragesCategory = categories.FirstOrDefault(x => x.CategoryName.Equals("Beverages", StringComparison.OrdinalIgnoreCase));
        var sourceName = external.ProductName!;
        var suggestedName = string.IsNullOrWhiteSpace(external.Brands)
            ? sourceName
            : $"{sourceName} ({external.Brands})";
        return new ExternalProductImportViewModel
        {
            Barcode = external.Code,
            ProductName = suggestedName.Length <= 40 ? suggestedName : sourceName[..Math.Min(sourceName.Length, 40)],
            Quantity = external.Quantity,
            SourceCategory = external.Categories,
            ImageUrl = external.ImageUrl,
            CategoryId = beveragesCategory?.CategoryId,
            Categories = categories
        };
    }

    private Task<List<Category>> GetCategories() => database.Categories.AsNoTracking()
        .OrderBy(x => x.CategoryName).ToListAsync();

    private static bool IsValidBarcode(string? barcode) => !string.IsNullOrWhiteSpace(barcode) &&
        barcode.Length <= 32 && barcode.All(char.IsAsciiDigit);

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
