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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<FuzzAiConfig>().ToTable("FuzzAIConfigs");
        builder.Entity<FuzzAiModel>().ToTable("FuzzAiModels");
        
        builder.Entity<FuzzUser>().ToTable("FuzzUsers");
        builder.Entity<IdentityRole>().ToTable("FuzzRoles");
        builder.Entity<IdentityUserRole<string>>().ToTable("FuzzUserRoles");
        builder.Entity<IdentityUserClaim<string>>().ToTable("FuzzUserClaims");
        builder.Entity<IdentityUserLogin<string>>().ToTable("FuzzUserLogins");
        builder.Entity<IdentityRoleClaim<string>>().ToTable("FuzzRoleClaims");
        builder.Entity<IdentityUserToken<string>>().ToTable("FuzzUserTokens");
    }
}
