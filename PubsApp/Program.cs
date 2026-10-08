using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using PubsApp.Data;

var builder = WebApplication.CreateBuilder(args);
StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<NorthwindContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Northwind")));
builder.Services.AddScoped<NorthwindProcedures>();
builder.Services.AddHttpClient<OpenFoodFactsClient>((services, client) =>
{
    var configuration = services.GetRequiredService<IConfiguration>();
    var baseAddress = configuration["OpenFoodFacts:BaseUrl"] ?? "https://world.openfoodfacts.org/";
    client.BaseAddress = new Uri(baseAddress);
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("PubsApp/1.0 (local learning project)");
    if (new Uri(baseAddress).Host.Equals("world.openfoodfacts.net", StringComparison.OrdinalIgnoreCase))
    {
        var credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("off:off"));
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
    }
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
