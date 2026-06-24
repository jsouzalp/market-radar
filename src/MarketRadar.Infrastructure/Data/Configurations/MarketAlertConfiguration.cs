using MarketRadar.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketRadar.Infrastructure.Data.Configurations;

public class MarketAlertConfiguration : IEntityTypeConfiguration<MarketAlert>
{
    public void Configure(EntityTypeBuilder<MarketAlert> builder)
    {
        builder.ToTable("MarketAlerts");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Symbol)
               .HasMaxLength(20)
               .IsRequired();

        builder.Property(a => a.Timeframe)
               .HasMaxLength(10)
               .IsRequired();

        builder.Property(a => a.Message)
               .HasMaxLength(500);

        builder.Property(a => a.Score)
               .HasColumnType("DECIMAL(18,8)");

        builder.HasIndex(a => new { a.Symbol, a.CreatedAt });
    }
}
