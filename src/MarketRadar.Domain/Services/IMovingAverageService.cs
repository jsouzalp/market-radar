using MarketRadar.Domain.Entities;
using MarketRadar.Domain.Results;

namespace MarketRadar.Domain.Services;

public interface IMovingAverageService
{
    IReadOnlyCollection<MovingAverageResult> CalculateEma(
        IReadOnlyList<MarketCandle> candles,
        IReadOnlyCollection<int> periods);
}
