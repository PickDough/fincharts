using System;
using app_core.dto;
using DotNext;

namespace app_core.service;

public class PriceService(
    IAssetsHistoricalPriceProvider historicalPriceProvider,
    AssetService assetService
)
{
    public async Task<Result<AssetHistoricalPrice>> GetAssetHistoricalPrices(
        Guid assetId,
        Provider provider,
        int Interval,
        DateInterval periodicity,
        int Count
    )
    {
        var assetResult = await assetService.GetAssetByIdAndProvider(assetId, provider);
        if (!assetResult.TryGet(out var asset))
            return Result.FromException<AssetHistoricalPrice>(assetResult.Error!);

        var prices = await historicalPriceProvider.GetAssetHistoricalPrices(
            asset,
            provider,
            Interval,
            periodicity,
            Count
        );

        return prices.Convert(p => new AssetHistoricalPrice(asset, p));
    }
}
