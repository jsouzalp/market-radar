namespace MarketRadar.Domain.Results;

public sealed record CandleValidationResult(
    bool IsValid,
    IReadOnlyCollection<string> Errors);
