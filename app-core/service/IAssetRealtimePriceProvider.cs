using System;
using app_core.dto;
using DotNext;

namespace app_core.service;

public enum RealtimePriceKind
{
    Ask,
    Bid,
    Last,
}

public interface IAssetRealtimePriceProvider
{
    public IAsyncEnumerable<Result<AssetRealtimePrice>> GetAssetRealtimePrices(
        Asset asset,
        Provider provider,
        IEnumerable<RealtimePriceKind> kinds,
        CancellationToken cancellationToken
    );
}
