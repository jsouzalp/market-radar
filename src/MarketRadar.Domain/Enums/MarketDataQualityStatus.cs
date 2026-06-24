namespace MarketRadar.Domain.Enums;

public enum MarketDataQualityStatus
{
    Unknown                = 0,
    Valid                  = 1,
    WaitingForEnoughData   = 2,
    ProviderUnavailable    = 3,
    InvalidCandlesReceived = 4,
    StaleData              = 5,
    MarketClosed           = 6
}
