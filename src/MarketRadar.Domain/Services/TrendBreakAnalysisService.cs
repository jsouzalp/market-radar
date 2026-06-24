using MarketRadar.Domain.Entities;
using MarketRadar.Domain.Enums;
using MarketRadar.Domain.Results;
using MarketRadar.Domain.Settings;

namespace MarketRadar.Domain.Services;

public class TrendBreakAnalysisService : ITrendBreakAnalysisService
{
    private readonly ITrendLineService _trendLineService;
    private readonly IMovingAverageService _movingAverageService;
    private readonly ITrendScoreService _trendScoreService;

    public TrendBreakAnalysisService(
        ITrendLineService trendLineService,
        IMovingAverageService movingAverageService,
        ITrendScoreService trendScoreService)
    {
        _trendLineService      = trendLineService;
        _movingAverageService  = movingAverageService;
        _trendScoreService     = trendScoreService;
    }

    public TrendBreakAnalysisResult Analyze(
        IReadOnlyList<MarketCandle> candles,
        TrendAnalysisSettings trendSettings,
        MovingAverageSettings movingAverageSettings)
    {
        string symbol    = candles[candles.Count - 1].Symbol;
        string timeframe = candles[candles.Count - 1].Timeframe;

        var trendLine      = _trendLineService.Calculate(candles, trendSettings);
        var movingAverages = _movingAverageService.CalculateEma(candles, movingAverageSettings.Periods);

        if (trendLine.Direction == TrendDirection.Neutral)
        {
            return new TrendBreakAnalysisResult(
                Symbol:         symbol,
                Timeframe:      timeframe,
                Status:         TrendAnalysisStatus.NeutralTrend,
                HasAlert:       false,
                AlertType:      null,
                Severity:       null,
                Score:          0m,
                TrendLine:      trendLine,
                MovingAverages: movingAverages,
                Message:        "Trend is neutral. Breakout analysis requires a defined up or down trend.");
        }

        bool isBreakDown = trendLine.Direction == TrendDirection.Up;
        decimal score    = _trendScoreService.CalculateScore(trendLine, movingAverages, candles, trendSettings);

        bool kCandlesConfirmed = CheckKConsecutiveCandles(candles, trendLine, trendSettings, isBreakDown);
        bool distanceConfirmed = CheckAverageDistance(candles, trendLine, trendSettings, isBreakDown);
        bool ema21Confirmed    = CheckEma21(movingAverages, candles[candles.Count - 1].ClosePrice, isBreakDown);
        bool scoreConfirmed    = score >= trendSettings.MinimumBreakScore;

        bool hasBreakout = kCandlesConfirmed && distanceConfirmed && ema21Confirmed && scoreConfirmed;

        if (!hasBreakout)
        {
            return new TrendBreakAnalysisResult(
                Symbol:         symbol,
                Timeframe:      timeframe,
                Status:         TrendAnalysisStatus.NoBreakout,
                HasAlert:       false,
                AlertType:      null,
                Severity:       null,
                Score:          score,
                TrendLine:      trendLine,
                MovingAverages: movingAverages,
                Message:        BuildNoBreakoutMessage(score, trendSettings.MinimumBreakScore, kCandlesConfirmed, distanceConfirmed, ema21Confirmed));
        }

        AlertType alertType = isBreakDown ? AlertType.TrendBreakDown : AlertType.TrendBreakUp;
        AlertSeverity severity = MapSeverity(score);

        return new TrendBreakAnalysisResult(
            Symbol:         symbol,
            Timeframe:      timeframe,
            Status:         TrendAnalysisStatus.BreakoutDetected,
            HasAlert:       true,
            AlertType:      alertType,
            Severity:       severity,
            Score:          score,
            TrendLine:      trendLine,
            MovingAverages: movingAverages,
            Message:        $"{alertType} detected. Score: {score:F0}. Severity: {severity}.");
    }

    // Conditions 1–2: last K candles all closed on the breaking side of the trend line
    private static bool CheckKConsecutiveCandles(
        IReadOnlyList<MarketCandle> candles,
        TrendLineResult trendLine,
        TrendAnalysisSettings settings,
        bool isBreakDown)
    {
        int n = candles.Count;
        int k = Math.Min(settings.RequiredBreakCandles, n);

        for (int i = n - k; i < n; i++)
        {
            decimal trendPriceAtI = trendLine.Intercept + trendLine.Slope * i;
            bool broke = isBreakDown
                ? candles[i].ClosePrice < trendPriceAtI
                : candles[i].ClosePrice > trendPriceAtI;
            if (!broke) return false;
        }

        return true;
    }

    // Condition 3: average distance of the last K candles > breakThreshold
    private static bool CheckAverageDistance(
        IReadOnlyList<MarketCandle> candles,
        TrendLineResult trendLine,
        TrendAnalysisSettings settings,
        bool isBreakDown)
    {
        decimal breakThreshold = trendLine.ResidualStandardDeviation * settings.DeviationMultiplier;
        int n = candles.Count;
        int k = Math.Min(settings.RequiredBreakCandles, n);
        decimal totalDistance = 0m;

        for (int i = n - k; i < n; i++)
        {
            decimal trendPriceAtI = trendLine.Intercept + trendLine.Slope * i;
            decimal dist = isBreakDown
                ? trendPriceAtI - candles[i].ClosePrice
                : candles[i].ClosePrice - trendPriceAtI;
            totalDistance += dist;
        }

        return k > 0 && totalDistance / k > breakThreshold;
    }

    // Condition 4: current close is on the breaking side of EMA 21
    private static bool CheckEma21(
        IReadOnlyCollection<MovingAverageResult> movingAverages,
        decimal currentClose,
        bool isBreakDown)
    {
        var ema21 = movingAverages.FirstOrDefault(m => m.Period == 21);
        if (ema21 is null) return false;

        return isBreakDown
            ? currentClose < ema21.CurrentValue
            : currentClose > ema21.CurrentValue;
    }

    private static AlertSeverity MapSeverity(decimal score) => score switch
    {
        >= 85 => AlertSeverity.Critical,
        >= 70 => AlertSeverity.Warning,
        _     => AlertSeverity.Info
    };

    private static string BuildNoBreakoutMessage(
        decimal score,
        decimal minimumScore,
        bool kCandlesConfirmed,
        bool distanceConfirmed,
        bool ema21Confirmed)
    {
        if (!kCandlesConfirmed)
            return $"Not enough consecutive candles confirmed the break. Score: {score:F0}.";
        if (!distanceConfirmed)
            return $"Break distance below statistical threshold. Score: {score:F0}.";
        if (!ema21Confirmed)
            return $"EMA 21 not confirmed. Score: {score:F0}.";
        return $"Score {score:F0} below minimum required ({minimumScore:F0}).";
    }
}
