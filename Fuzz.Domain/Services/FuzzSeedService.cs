using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Fuzz.Domain.Data;
using Fuzz.Domain.Entities;

namespace Fuzz.Domain.Services;

public interface IFuzzSeedService
{
    Task ApplyMigrationsAsync();
    Task SeedDataAsync();
}

public class FuzzSeedService : IFuzzSeedService
{
    private readonly FuzzDbContext _dbContext;
    private readonly UserManager<FuzzUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<FuzzSeedService> _logger;

    public FuzzSeedService(
        FuzzDbContext dbContext, 
        UserManager<FuzzUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<FuzzSeedService> logger)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task ApplyMigrationsAsync()
    {
        try
        {
            _logger.LogInformation("🔄 Checking database connection...");
            
            var canConnect = await _dbContext.Database.CanConnectAsync();
            
            if (canConnect)
            {
                _logger.LogInformation("✅ PostgreSQL connection successful!");
                
                var pendingMigrations = await _dbContext.Database.GetPendingMigrationsAsync();
                
                if (pendingMigrations.Any())
                {
                    _logger.LogInformation("📦 {Count} pending migrations found. Applying...", pendingMigrations.Count());
                    
                    await _dbContext.Database.MigrateAsync();
                    
                    _logger.LogInformation("✅ All migrations applied successfully!");
                }
                else
                {
                    _logger.LogInformation("✅ Database is up to date.");
                }
            }
            else
            {
                _logger.LogWarning("⚠️ Could not connect to PostgreSQL. Skipping database operations.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Database migration error: {Message}", ex.Message);
        }
    }

    public async Task SeedDataAsync()
    {
        try
        {
            if (!await _dbContext.Database.CanConnectAsync())
            {
                _logger.LogWarning("⚠️ Skipping seed - no database connection.");
                return;
            }

            // 1. Create Roles and Admin
            await SeedRolesAndAdminAsync();

            // 2. Seed Northwind Data
            await SeedNorthwindDataAsync();

            // 3. Upsert Default Models (Hybrid Support)
            // We merge Text and Visual definitions into single entities with IsVisualRecognition=true
            var defaultModels = new List<FuzzAiModel>
            {
                // Gemini (Vision Capable)
                new() { Provider = AiProvider.Gemini, ModelId = "gemini-1.5-flash", DisplayName = "Gemini 1.5 Flash", Capabilities = AiCapabilities.Text | AiCapabilities.Visual },
                new() { Provider = AiProvider.Gemini, ModelId = "gemini-1.5-pro", DisplayName = "Gemini 1.5 Pro", Capabilities = AiCapabilities.Text | AiCapabilities.Visual },
                new() { Provider = AiProvider.Gemini, ModelId = "gemini-2.0-flash", DisplayName = "Gemini 2.0 Flash", Capabilities = AiCapabilities.Text | AiCapabilities.Visual },
                
                // OpenAI - Frontier Models (Advanced)
                new() { Provider = AiProvider.OpenAI, ModelId = "gpt-5.2", DisplayName = "GPT-5.2", Capabilities = AiCapabilities.Text | AiCapabilities.Visual },
                new() { Provider = AiProvider.OpenAI, ModelId = "gpt-5.2-pro", DisplayName = "GPT-5.2 Pro", Capabilities = AiCapabilities.Text | AiCapabilities.Visual },
                new() { Provider = AiProvider.OpenAI, ModelId = "gpt-5", DisplayName = "GPT-5", Capabilities = AiCapabilities.Text | AiCapabilities.Visual },
                new() { Provider = AiProvider.OpenAI, ModelId = "gpt-5-mini", DisplayName = "GPT-5 Mini", Capabilities = AiCapabilities.Text | AiCapabilities.Visual },
                new() { Provider = AiProvider.OpenAI, ModelId = "gpt-5-nano", DisplayName = "GPT-5 Nano", Capabilities = AiCapabilities.Text | AiCapabilities.Visual },
                
                // OpenAI - Current/Legacy
                new() { Provider = AiProvider.OpenAI, ModelId = "gpt-4.1", DisplayName = "GPT-4.1", Capabilities = AiCapabilities.Text | AiCapabilities.Visual },
                new() { Provider = AiProvider.OpenAI, ModelId = "gpt-4.1-mini", DisplayName = "GPT-4.1 Mini", Capabilities = AiCapabilities.Text | AiCapabilities.Visual },
                new() { Provider = AiProvider.OpenAI, ModelId = "gpt-4.1-nano", DisplayName = "GPT-4.1 Nano", Capabilities = AiCapabilities.Text | AiCapabilities.Visual },
                new() { Provider = AiProvider.OpenAI, ModelId = "o4-mini", DisplayName = "o4 Mini", Capabilities = AiCapabilities.Text | AiCapabilities.Visual },
                
                new() { Provider = AiProvider.OpenAI, ModelId = "gpt-4o", DisplayName = "GPT-4o", Capabilities = AiCapabilities.Text | AiCapabilities.Visual },
                new() { Provider = AiProvider.OpenAI, ModelId = "gpt-4o-mini", DisplayName = "GPT-4o Mini", Capabilities = AiCapabilities.Text | AiCapabilities.Visual },
                
                // OpenAI - Open Weight (Simulated)
                new() { Provider = AiProvider.OpenAI, ModelId = "gpt-oss-120b", DisplayName = "GPT-OSS 120B", Capabilities = AiCapabilities.Text },
                new() { Provider = AiProvider.OpenAI, ModelId = "gpt-oss-20b", DisplayName = "GPT-OSS 20B", Capabilities = AiCapabilities.Text },

                // ElevenLabs (Vocal Agents)
                new() { Provider = AiProvider.ElevenLabs, ModelId = "TxGEqnHWf9848Y7l7R7i", DisplayName = "ElevenLabs - Josh (Vocal)", Capabilities = AiCapabilities.Sound },
                new() { Provider = AiProvider.ElevenLabs, ModelId = "EXAVITQu4vr4xnSDxMaL", DisplayName = "ElevenLabs - Bella (Vocal)", Capabilities = AiCapabilities.Sound },

                // Replicate (MusicGen)
                new() { Provider = AiProvider.Replicate, ModelId = "meta/musicgen:stereo-melody-large", DisplayName = "MusicGen Stereo Melody Large", Capabilities = AiCapabilities.Sound },
                new() { Provider = AiProvider.Replicate, ModelId = "meta/musicgen:stereo-large", DisplayName = "MusicGen Stereo Large", Capabilities = AiCapabilities.Sound }
            };

            foreach (var model in defaultModels)
            {
                var existing = await _dbContext.FuzzAiModels
                    .FirstOrDefaultAsync(m => m.Provider == model.Provider && m.ModelId == model.ModelId);

                if (existing == null)
                {
                    _logger.LogInformation("➕ Adding missing model: {ModelName}", model.DisplayName);
                    _dbContext.FuzzAiModels.Add(model);
                }
                else
                {
                    bool changed = false;
                    // Update capabilities
                    if (existing.Capabilities != model.Capabilities)
                    {
                        existing.Capabilities = model.Capabilities;
                        changed = true;
                    }
                    if (existing.DisplayName != model.DisplayName)
                    {
                        existing.DisplayName = model.DisplayName;
                        changed = true;
                    }

                    if (changed)
                    {
                        _dbContext.Entry(existing).State = EntityState.Modified;
                    }
                }
            }

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("✅ Available models synchronized.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Seed error: {Message}", ex.Message);
        }
    }

    private async Task SeedRolesAndAdminAsync()
    {
        if (!await _roleManager.RoleExistsAsync("Admin"))
        {
            _logger.LogInformation("🔑 Creating 'Admin' role...");
            await _roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        var adminEmail = "admin@fuzz.com";
        var adminUser = await _userManager.FindByEmailAsync(adminEmail);
        
        if (adminUser == null)
        {
            _logger.LogInformation("👤 Creating default admin user...");
            adminUser = new FuzzUser 
            { 
                UserName = adminEmail, 
                Email = adminEmail, 
                EmailConfirmed = true 
            };
            
            var result = await _userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(adminUser, "Admin");
                _logger.LogInformation("✅ Admin user created and assigned to 'Admin' role.");
            }
        }
    }

    private async Task SeedNorthwindDataAsync()
    {
        if (await _dbContext.Categories.AnyAsync())
        {
            _logger.LogInformation("✅ Northwind data already seeded.");
            return;
        }

        _logger.LogInformation("🌱 Seeding Northwind data...");

        // Categories
        var categories = new List<FuzzCategory>
        {
            new() { CategoryName = "Beverages", Description = "Soft drinks, coffees, teas, beers, and ales" },
            new() { CategoryName = "Condiments", Description = "Sweet and savory sauces, relishes, spreads, and seasonings" },
            new() { CategoryName = "Confections", Description = "Desserts, candies, and sweet breads" },
            new() { CategoryName = "Dairy Products", Description = "Cheeses" },
            new() { CategoryName = "Grains/Cereals", Description = "Breads, crackers, pasta, and cereal" },
            new() { CategoryName = "Meat/Poultry", Description = "Prepared meats" },
            new() { CategoryName = "Produce", Description = "Dried fruit and bean curd" },
            new() { CategoryName = "Seafood", Description = "Seaweed and fish" }
        };
        _dbContext.Categories.AddRange(categories);
        await _dbContext.SaveChangesAsync();

        // Shippers
        var shippers = new List<FuzzShipper>
        {
            new() { CompanyName = "Speedy Express", Phone = "(503) 555-9831" },
            new() { CompanyName = "United Package", Phone = "(503) 555-3199" },
            new() { CompanyName = "Federal Shipping", Phone = "(503) 555-9931" }
        };
        _dbContext.Shippers.AddRange(shippers);
        await _dbContext.SaveChangesAsync();

        // Customers (A few samples)
        var customers = new List<FuzzCustomer>
        {
            new() { CustomerID = "ALFKI", CompanyName = "Alfreds Futterkiste", ContactName = "Maria Anders", City = "Berlin", Country = "Germany" },
            new() { CustomerID = "ANATR", CompanyName = "Ana Trujillo Emparedados y helados", ContactName = "Ana Trujillo", City = "México D.F.", Country = "Mexico" },
            new() { CustomerID = "ANTON", CompanyName = "Antonio Moreno Taquería", ContactName = "Antonio Moreno", City = "México D.F.", Country = "Mexico" },
            new() { CustomerID = "AROUT", CompanyName = "Around the Horn", ContactName = "Thomas Hardy", City = "London", Country = "UK" },
            new() { CustomerID = "BERGS", CompanyName = "Berglunds snabbköp", ContactName = "Christina Berglund", City = "Luleå", Country = "Sweden" }
        };
        _dbContext.Customers.AddRange(customers);
        await _dbContext.SaveChangesAsync();

        // Employees (Minimal set)
        var employees = new List<FuzzEmployee>
        {
            new() { FirstName = "Nancy", LastName = "Davolio", Title = "Sales Representative", City = "Seattle", Country = "USA" },
            new() { FirstName = "Andrew", LastName = "Fuller", Title = "Vice President, Sales", City = "Tacoma", Country = "USA" }
        };
        _dbContext.Employees.AddRange(employees);
        await _dbContext.SaveChangesAsync();

        // Products (Sample linked to Category 1)
        var bevCat = categories.First(c => c.CategoryName == "Beverages");
        var products = new List<FuzzProduct>
        {
            new() { ProductName = "Chai", CategoryID = bevCat.CategoryID, UnitPrice = 18.00m, UnitsInStock = 39 },
            new() { ProductName = "Chang", CategoryID = bevCat.CategoryID, UnitPrice = 19.00m, UnitsInStock = 17 },
            new() { ProductName = "Aniseed Syrup", CategoryID = bevCat.CategoryID, UnitPrice = 10.00m, UnitsInStock = 13 }
        };
        _dbContext.Products.AddRange(products);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("✅ Northwind data seeding completed.");
    }
}
