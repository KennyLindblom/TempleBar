using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace PubsApp.Data;

public sealed class OpenFoodFactsClient(HttpClient httpClient)
{
    public async Task<OpenFoodFactsSearchResponse> SearchAsync(string searchTerm, CancellationToken cancellationToken)
    {
        var query = await new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["search_terms"] = searchTerm,
            ["search_simple"] = "1",
            ["action"] = "process",
            ["json"] = "1",
            ["page_size"] = "20",
            ["fields"] = "code,product_name,brands,quantity,categories,image_url"
        }).ReadAsStringAsync(cancellationToken);

        using var response = await httpClient.GetAsync($"cgi/search.pl?{query}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode == 503)
            throw new HttpRequestException("Open Food Facts is temporarily rate limiting requests. Please wait a moment and try again.");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OpenFoodFactsSearchResponse>(cancellationToken: cancellationToken)
            ?? new OpenFoodFactsSearchResponse();
    }

    public async Task<OpenFoodFactsProduct?> GetProductAsync(string barcode, CancellationToken cancellationToken)
    {
        var encodedBarcode = Uri.EscapeDataString(barcode);
        using var response = await httpClient.GetAsync(
            $"api/v3.6/product/{encodedBarcode}.json?fields=code,product_name,brands,quantity,categories,image_url",
            cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<OpenFoodFactsProductResponse>(cancellationToken: cancellationToken);
        return result is not null && result.Status.StartsWith("success", StringComparison.OrdinalIgnoreCase)
            ? result.Product
            : null;
    }
}

public sealed class OpenFoodFactsSearchResponse
{
    [JsonPropertyName("count")] public int Count { get; set; }
    [JsonPropertyName("products")] public List<OpenFoodFactsProduct> Products { get; set; } = [];
}

public sealed class OpenFoodFactsProductResponse
{
    // API v3 uses values such as "success" and "success_with_errors"; v2 used numeric status codes.
    [JsonPropertyName("status")] public string Status { get; set; } = "";
    [JsonPropertyName("product")] public OpenFoodFactsProduct? Product { get; set; }
}

public sealed class OpenFoodFactsProduct
{
    [JsonPropertyName("code")] public string Code { get; set; } = "";
    [JsonPropertyName("product_name")] public string? ProductName { get; set; }
    [JsonPropertyName("brands")] public string? Brands { get; set; }
    [JsonPropertyName("quantity")] public string? Quantity { get; set; }
    [JsonPropertyName("categories")] public string? Categories { get; set; }
    [JsonPropertyName("image_url")] public string? ImageUrl { get; set; }
}
