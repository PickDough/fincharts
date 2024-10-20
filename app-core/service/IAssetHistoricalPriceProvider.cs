using System;
using app_core.dto;
using DotNext;

namespace app_core.service;

public enum DateInterval
{
    Year,
    Quarter,
    Month,
    Day,
    Hour,
    Minute,
}

public interface IAssetsHistoricalPriceProvider
{
    Task<Result<IEnumerable<OhlcBar>>> GetAssetHistoricalPrices(
        Asset asset,
        Provider provider,
        int Interval,
        DateInterval periodicity,
        int Count
    );
}
