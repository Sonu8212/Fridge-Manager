using FridgeManager.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FridgeManager.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<FridgeItem> FridgeItems => Set<FridgeItem>();
    public DbSet<ConsumptionLog> ConsumptionLogs => Set<ConsumptionLog>();
    public DbSet<ShoppingListItem> ShoppingListItems => Set<ShoppingListItem>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FridgeItem>(e =>
        {
            e.Property(x => x.CostPerUnit).HasPrecision(18, 2);
            e.Property(x => x.TotalCost).HasPrecision(18, 2);
            e.Property(x => x.Quantity).HasPrecision(18, 3);
        });

        modelBuilder.Entity<ConsumptionLog>(e =>
        {
            e.Property(x => x.QuantityUsed).HasPrecision(18, 3);
            e.HasOne(x => x.FridgeItem)
             .WithMany(x => x.ConsumptionLogs)
             .HasForeignKey(x => x.FridgeItemId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ShoppingListItem>(e =>
        {
            e.Property(x => x.Quantity).HasPrecision(18, 3);
            e.Property(x => x.EstimatedCost).HasPrecision(18, 2);
        });
    }
}
