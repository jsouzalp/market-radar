using MarketRadar.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketRadar.Infrastructure.Data;

public class MarketRadarDbContext : DbContext
{
    public DbSet<MarketCandle> Candles => Set<MarketCandle>();
    public DbSet<MarketAlert>  Alerts  => Set<MarketAlert>();

    public MarketRadarDbContext(DbContextOptions<MarketRadarDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(MarketRadarDbContext).Assembly);
}
