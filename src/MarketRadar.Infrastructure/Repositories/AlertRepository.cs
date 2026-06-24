using MarketRadar.Application.Contracts.Repositories;
using MarketRadar.Domain.Entities;
using MarketRadar.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketRadar.Infrastructure.Repositories;

public sealed class AlertRepository : IAlertRepository
{
    private readonly MarketRadarDbContext _context;

    public AlertRepository(MarketRadarDbContext context) => _context = context;

    public async Task AddAsync(MarketAlert alert, CancellationToken cancellationToken)
    {
        alert.CreatedAt = DateTime.UtcNow;
        _context.Alerts.Add(alert);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<MarketAlert>> GetRecentAsync(
        string symbol,
        int count,
        CancellationToken cancellationToken)
    {
        return await _context.Alerts
            .Where(a => a.Symbol == symbol)
            .OrderByDescending(a => a.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}
