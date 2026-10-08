using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace PubsApp.Data;

public sealed class NorthwindProcedures(NorthwindContext database)
{
    public Task<List<OrderLineResult>> GetOrderDetailsAsync(int orderId) => QueryAsync(
        "dbo.CustOrdersDetail",
        command => AddParameter(command, "@OrderID", DbType.Int32, orderId),
        reader => new OrderLineResult(
            reader.GetString(reader.GetOrdinal("ProductName")),
            reader.GetDecimal(reader.GetOrdinal("UnitPrice")),
            Convert.ToInt32(reader.GetValue(reader.GetOrdinal("Quantity"))),
            Convert.ToInt32(reader.GetValue(reader.GetOrdinal("Discount"))),
            reader.GetDecimal(reader.GetOrdinal("ExtendedPrice"))));

    public Task<List<CustomerOrderResult>> GetCustomerOrdersAsync(string customerId) => QueryAsync(
        "dbo.CustOrdersOrders",
        command => AddParameter(command, "@CustomerID", DbType.StringFixedLength, customerId, 5),
        reader => new CustomerOrderResult(
            reader.GetInt32(reader.GetOrdinal("OrderID")),
            GetNullableDate(reader, "OrderDate"),
            GetNullableDate(reader, "RequiredDate"),
            GetNullableDate(reader, "ShippedDate")));

    public Task<List<CustomerProductHistoryResult>> GetCustomerProductHistoryAsync(string customerId) => QueryAsync(
        "dbo.CustOrderHist",
        command => AddParameter(command, "@CustomerID", DbType.StringFixedLength, customerId, 5),
        reader => new CustomerProductHistoryResult(
            reader.GetString(reader.GetOrdinal("ProductName")),
            Convert.ToInt32(reader.GetValue(reader.GetOrdinal("Total")))));

    public Task<List<SalesByYearResult>> GetSalesByYearAsync(DateTime from, DateTime to) => QueryAsync(
        "dbo.[Sales by Year]",
        command =>
        {
            AddParameter(command, "@Beginning_Date", DbType.DateTime, from);
            AddParameter(command, "@Ending_Date", DbType.DateTime, to);
        },
        reader => new SalesByYearResult(
            GetNullableDate(reader, "ShippedDate"),
            reader.GetInt32(reader.GetOrdinal("OrderID")),
            reader.GetDecimal(reader.GetOrdinal("Subtotal")),
            reader.GetString(reader.GetOrdinal("Year"))));

    public Task<List<CategoryProductSalesResult>> GetSalesByCategoryAsync(string categoryName, string year) => QueryAsync(
        "dbo.SalesByCategory",
        command =>
        {
            AddParameter(command, "@CategoryName", DbType.String, categoryName, 15);
            AddParameter(command, "@OrdYear", DbType.String, year, 4);
        },
        reader => new CategoryProductSalesResult(
            reader.GetString(reader.GetOrdinal("ProductName")),
            reader.GetDecimal(reader.GetOrdinal("TotalPurchase"))));

    public Task<List<ExpensiveProductResult>> GetTenMostExpensiveProductsAsync() => QueryAsync(
        "dbo.[Ten Most Expensive Products]",
        _ => { },
        reader => new ExpensiveProductResult(
            reader.GetString(reader.GetOrdinal("TenMostExpensiveProducts")),
            reader.IsDBNull(reader.GetOrdinal("UnitPrice")) ? null : reader.GetDecimal(reader.GetOrdinal("UnitPrice"))));

    private async Task<List<T>> QueryAsync<T>(string procedure, Action<DbCommand> configure, Func<DbDataReader, T> map)
    {
        var connection = database.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;
        if (openedHere)
            await database.Database.OpenConnectionAsync();

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = procedure;
            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = 30;
            configure(command);

            await using var reader = await command.ExecuteReaderAsync();
            var results = new List<T>();
            while (await reader.ReadAsync())
                results.Add(map(reader));
            return results;
        }
        finally
        {
            if (openedHere)
                await database.Database.CloseConnectionAsync();
        }
    }

    private static void AddParameter(DbCommand command, string name, DbType type, object value, int? size = null)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = type;
        parameter.Value = value;
        if (size.HasValue)
            parameter.Size = size.Value;
        command.Parameters.Add(parameter);
    }

    private static DateTime? GetNullableDate(DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }
}

public record OrderLineResult(string ProductName, decimal UnitPrice, int Quantity, int DiscountPercent, decimal ExtendedPrice);
public record CustomerOrderResult(int OrderId, DateTime? OrderDate, DateTime? RequiredDate, DateTime? ShippedDate);
public record CustomerProductHistoryResult(string ProductName, int Total);
public record SalesByYearResult(DateTime? ShippedDate, int OrderId, decimal Subtotal, string Year);
public record CategoryProductSalesResult(string ProductName, decimal TotalPurchase);
public record ExpensiveProductResult(string ProductName, decimal? UnitPrice);
