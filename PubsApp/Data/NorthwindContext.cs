using Microsoft.EntityFrameworkCore;
using PubsApp.Models;

namespace PubsApp.Data;

public class NorthwindContext(DbContextOptions<NorthwindContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Shipper> Shippers => Set<Shipper>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>().ToTable("Customers").HasKey(x => x.CustomerId);
        modelBuilder.Entity<Customer>().Property(x => x.CustomerId).HasColumnType("nchar(5)");
        modelBuilder.Entity<Customer>().Property(x => x.CompanyName).HasMaxLength(40).IsRequired();

        modelBuilder.Entity<Order>().ToTable("Orders").HasKey(x => x.OrderId);
        modelBuilder.Entity<Order>().Property(x => x.Freight).HasColumnType("money");
        modelBuilder.Entity<Order>().HasOne(x => x.Customer).WithMany(x => x.Orders)
            .HasForeignKey(x => x.CustomerId);
        modelBuilder.Entity<Order>().HasOne(x => x.Employee).WithMany()
            .HasForeignKey(x => x.EmployeeId);
        modelBuilder.Entity<Order>().HasOne(x => x.Shipper).WithMany()
            .HasForeignKey(x => x.ShipVia);

        modelBuilder.Entity<OrderDetail>().ToTable("Order Details").HasKey(x => new { x.OrderId, x.ProductId });
        modelBuilder.Entity<OrderDetail>().Property(x => x.UnitPrice).HasColumnType("money");
        modelBuilder.Entity<OrderDetail>().HasOne(x => x.Order).WithMany(x => x.OrderDetails)
            .HasForeignKey(x => x.OrderId);
        modelBuilder.Entity<OrderDetail>().HasOne(x => x.Product).WithMany(x => x.OrderDetails)
            .HasForeignKey(x => x.ProductId);

        modelBuilder.Entity<Product>().ToTable("Products").HasKey(x => x.ProductId);
        modelBuilder.Entity<Product>().Property(x => x.UnitPrice).HasColumnType("money");
        modelBuilder.Entity<Product>().HasOne(x => x.Category).WithMany()
            .HasForeignKey(x => x.CategoryId);

        modelBuilder.Entity<Category>().ToTable("Categories").HasKey(x => x.CategoryId);
        modelBuilder.Entity<Employee>().ToTable("Employees").HasKey(x => x.EmployeeId);
        modelBuilder.Entity<Shipper>().ToTable("Shippers").HasKey(x => x.ShipperId);
    }
}
