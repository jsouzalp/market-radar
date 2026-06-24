# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

**Market Radar** — a .NET 10 Blazor Server application that monitors XAUUSD (gold) for technical trend breakouts and generates visual/audio alerts. It collects M1 candles, calculates linear regression trend lines and EMAs (9, 21, 50), detects breakouts, and scores alerts (0–100). No trading execution — alerts only.

## Commands

```bash
# Build
dotnet build MarketRadar.slnx

# Run the web app
dotnet run --project src/MarketRadar.Web

# Run all tests (82 total: 58 domain + 24 integration)
dotnet test MarketRadar.slnx

# Run tests for a specific project
dotnet test tests/MarketRadar.Domain.Tests
dotnet test tests/MarketRadar.Application.Tests

# Run a single test by name
dotnet test --filter "FullyQualifiedName~TrendLineServiceTests.ShouldDetectUpTrend"
```

> The solution file is `MarketRadar.slnx` (not `.sln`).

## Architecture

### Dependency rule

`Web` → `Application` → `Domain`. `Infrastructure` implements contracts defined in `Application`. `Domain` has zero external dependencies.

Settings classes live in **`Application/Settings/`** (not Infrastructure) so Application services can inject them via `IOptions<T>`. The exception is `AlertSettings` (sound/visual), which is Infrastructure-only.

### Data flow (background worker cycle)

```
MarketMonitorWorker (60s polling)
→ IMarketDataProvider.GetLatestCandlesAsync()
    — if empty → IProviderStatusService.SetMarketClosed() → return
    — if data  → IProviderStatusService.SetMarketOpen()
→ IMarketCandleNormalizer.Normalize()       # symbol, timeframe, UTC, sort asc
→ ICandleValidator.Validate()               # drop invalid candles
→ ICandleRepository.AddOrUpdateAsync()      # upsert; key: (Symbol, Timeframe, OpenTime)
→ ICandleRepository.GetRecentAsync(120)     # fetch regression window
→ IMarketDataQualityService.Evaluate()      # min candles, staleness check
    — if !CanAnalyze → return
→ ITrendBreakAnalysisService.Analyze()      # regression + EMA + breakout + score
→ IAlertCooldownService.Evaluate()          # track consecutive alerts; may block
    — if !HasAlert → return
    — if !CanDispatch → log blocked + return
→ IAlertRepository.AddAsync()
→ IAlertDispatcher.DispatchAsync()          # Blazor event bus + optional sound
```

### Dashboard

`DashboardAppService.GetDashboardAsync()` checks `IProviderStatusService.IsMarketClosed()` first, then re-reads the DB and re-runs analysis on every request (no caching). Status priority: `MarketClosed` → `WaitingForEnoughData` → `StaleData` → `Online`.

### Alert cooldown

`AlertCooldownService` (singleton) tracks `(symbol, alertType) → (consecutive, quiet)` in a `ConcurrentDictionary`. Blocking condition: `consecutive > MaxConsecutiveAlerts`. After `MinCyclesToReset` consecutive quiet cycles for the **same alertType**, the counter resets. Note: quiet cycles in the monitoring loop use alertType `"None"` (no breakout detected), so the reset must be triggered by quiet cycles on the specific alertType key.

### MockMarketDataProvider

Caps at 500 candles per request. Key invariant for `TrendBreakDown` scenario: the break zone uses only the last **5 candles** so the 120-candle regression slope stays positive (uptrend required by condition 1). Changing the break zone to 30+ candles makes the slope negative and breaks detection.

## Domain concepts

### Breakout detection

**TrendBreakDown** requires all of:
1. Prior regression slope > `MinimumSlope` (uptrend)
2. Last `RequiredBreakCandles` closed below regression line
3. Average distance > `ResidualStdDev × DeviationMultiplier`
4. ClosePrice < EMA 21
5. Score ≥ `MinimumBreakScore` (default 70)

**TrendBreakUp** is the mirror condition.

### Alert score (0–100)

| Condition | Points |
|---|---|
| Broke regression line | +25 |
| Held break for K consecutive candles | +20 |
| Distance exceeds statistical threshold | +20 |
| Price broke EMA 21 | +15 |
| EMA 9 turned against prior trend | +10 |
| Broke recent high/low | +10 |

Score thresholds: 0–49 ignore, 50–69 attention, 70–84 alert, 85–100 strong alert.

## Configuration (`appsettings.json`)

```json
{
  "ConnectionStrings": { "MarketRadar": "Data Source=marketradar.db" },
  "MarketMonitor": {
    "HistoryDays": 30,
    "Symbols": [{ "Code": "XAUUSD", "Enabled": true, "Timeframe": "M1" }]
  },
  "MarketDataProvider": {
    "Primary": "Mock", "Comparison": null, "ActiveProvider": "Mock",
    "PollingIntervalSeconds": 60,
    "MockScenario": "TrendBreakDown",
    "UseOnlyClosedCandles": true, "RequestOverlapMinutes": 5
  },
  "TrendAnalysis": {
    "RegressionWindowCandles": 120, "RequiredBreakCandles": 3,
    "DeviationMultiplier": 1.5, "MinimumSlope": 0.001, "MinimumBreakScore": 70,
    "RecentHighLowWindowCandles": 20, "FalseBreakoutLookbackCandles": 5
  },
  "MovingAverages": { "Periods": [9, 21, 50] },
  "Alerts": { "EnableSound": true, "EnableVisualHighlight": true },
  "MarketDataQuality": {
    "MinimumRequiredCandles": 120,
    "StaleDataToleranceMinutes": 5,
    "UseOnlyClosedCandlesForAnalysis": true
  },
  "AlertCooldown": { "MaxConsecutiveAlerts": 5, "MinCyclesToReset": 3 }
}
```

`ActiveProvider` values: `Mock`, `MetaTrader`, `FotMarket`, `ExternalHttp`, `Csv`.
`MockScenario` values: `TrendBreakDown`, `TrendBreakUp`, `UpTrend`, `DownTrend`, `StableMarket`, `EmptyResponse`, `MarketClosed`, `ProviderFailure`.

## Database

SQLite (`Data Source=marketradar.db`). Two tables configured via EF Core Fluent API:

- `MarketCandles` — unique index on `(Symbol, Timeframe, OpenTime)`; all prices as `DECIMAL(18,8)`
- `MarketAlerts` — stores generated alerts

## Testing

### Domain tests (`MarketRadar.Domain.Tests`)

Pure unit tests, no DI, no DB. Use `CandleBuilder` helper (`tests/MarketRadar.Domain.Tests/Helpers/`) for test data. Framework: xUnit + FluentAssertions.

### Application integration tests (`MarketRadar.Application.Tests`)

Use `AppTestFixture` (`tests/MarketRadar.Application.Tests/Helpers/`) which:
- Opens a `SqliteConnection("Data Source=:memory:")` kept alive across scopes
- Calls `services.AddApplicationServices()` for all domain/app services
- Registers real `CandleRepository`, `AlertRepository`, `ProviderStatusService`, `AlertCooldownService`
- Uses `MockMarketDataProvider` with configurable scenario
- Injects `CapturingAlertDispatcher` to assert dispatched alerts

Each test creates a fresh `AppTestFixture` (fresh DB). Singletons like `IAlertCooldownService` persist across `CreateScope()` calls within the same fixture.

## Specifications

Design decisions and acceptance criteria are documented in `.specs/`:

| File | Content |
|---|---|
| `0001-market-radar-web-mvp.md` | Overall MVP scope, models, flows |
| `0002-market-data-provider.md` | Provider abstraction, mock scenarios, normalization |
| `0003-market-data-quality.md` | Candle validation, quality states |
| `0004-trend-analysis-engine.md` | Regression, EMA, breakout detection, scoring |
| `0005-dashboard-and-alerts.md` | Blazor UI, dashboard states, alert dispatch |
| `0006-architecture-decisions.md` | Alert cooldown, MarketClosed, config restructuring |
| `0007-implementation-plan.md` | Retrospective implementation phases |
