using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Fuzz.Domain.Entities;

namespace Fuzz.Domain.Data;

public class FuzzDbContext : IdentityDbContext<FuzzUser>
{
    public FuzzDbContext(DbContextOptions<FuzzDbContext> options) : base(options)
    {
    }

    public DbSet<FuzzTodo> Todos { get; set; }
    public DbSet<FuzzAiConfig> AiConfigurations { get; set; }
    public DbSet<FuzzAiModel> FuzzAiModels { get; set; }
    public DbSet<FuzzAiParameters> FuzzAiParameters { get; set; }

    // Northwind Tables
    public DbSet<FuzzCategory> Categories { get; set; }
    public DbSet<FuzzCustomer> Customers { get; set; }
    public DbSet<FuzzEmployee> Employees { get; set; }
    public DbSet<FuzzSupplier> Suppliers { get; set; }
    public DbSet<FuzzShipper> Shippers { get; set; }
    public DbSet<FuzzProduct> Products { get; set; }
    public DbSet<FuzzOrder> Orders { get; set; }
    public DbSet<FuzzOrderDetail> OrderDetails { get; set; }
    public DbSet<FuzzSqlLog> SqlLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<FuzzAiConfig>().ToTable("FuzzAIConfigs");
        builder.Entity<FuzzAiModel>().ToTable("FuzzAiModels");
        builder.Entity<FuzzAiParameters>().ToTable("FuzzAIParameters");
        builder.Entity<FuzzSqlLog>().ToTable("Fuzz_SqlLogs");

        // Northwind Configurations with Fuzz_ prefix
        builder.Entity<FuzzCategory>().ToTable("Fuzz_Categories");
        builder.Entity<FuzzCustomer>().ToTable("Fuzz_Customers");
        builder.Entity<FuzzEmployee>().ToTable("Fuzz_Employees");
        builder.Entity<FuzzSupplier>().ToTable("Fuzz_Suppliers");
        builder.Entity<FuzzShipper>().ToTable("Fuzz_Shippers");
        builder.Entity<FuzzProduct>().ToTable("Fuzz_Products");
        builder.Entity<FuzzOrder>().ToTable("Fuzz_Orders");
        builder.Entity<FuzzOrderDetail>().ToTable("Fuzz_OrderDetails");

        builder.Entity<FuzzOrderDetail>()
            .HasKey(od => new { od.OrderID, od.ProductID });
        
        builder.Entity<FuzzUser>().ToTable("FuzzUsers");
        builder.Entity<IdentityRole>().ToTable("FuzzRoles");
        builder.Entity<IdentityUserRole<string>>().ToTable("FuzzUserRoles");
        builder.Entity<IdentityUserClaim<string>>().ToTable("FuzzUserClaims");
        builder.Entity<IdentityUserLogin<string>>().ToTable("FuzzUserLogins");
        builder.Entity<IdentityRoleClaim<string>>().ToTable("FuzzRoleClaims");
        builder.Entity<IdentityUserToken<string>>().ToTable("FuzzUserTokens");
    }
}
