using MarketRadar.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketRadar.Infrastructure.Data.Configurations;

public class MarketCandleConfiguration : IEntityTypeConfiguration<MarketCandle>
{
    public void Configure(EntityTypeBuilder<MarketCandle> builder)
    {
        builder.ToTable("MarketCandles");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Symbol)
               .HasMaxLength(20)
               .IsRequired();

        builder.Property(c => c.Timeframe)
               .HasMaxLength(10)
               .IsRequired();

        builder.Property(c => c.OpenPrice)  .HasColumnType("DECIMAL(18,8)");
        builder.Property(c => c.HighPrice)  .HasColumnType("DECIMAL(18,8)");
        builder.Property(c => c.LowPrice)   .HasColumnType("DECIMAL(18,8)");
        builder.Property(c => c.ClosePrice) .HasColumnType("DECIMAL(18,8)");
        builder.Property(c => c.Volume)     .HasColumnType("DECIMAL(18,8)");

        builder.HasIndex(c => new { c.Symbol, c.Timeframe, c.OpenTime })
               .IsUnique();
    }
}
