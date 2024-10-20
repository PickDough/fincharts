using System;
using app_core.dto;
using app_core.repository;
using DotNext;

namespace app_core.service;

public class PriceService(
    IAssetsHistoricalPriceProvider historicalPriceProvider,
    IAssetRepository assetRepository
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
        var assetResult = await assetRepository.GetAsset(assetId);
        if (!assetResult.TryGet(out var asset))
            return Result.FromException<AssetHistoricalPrice>(new Exception("Failed to get asset"));

        if (!asset.Providers.Contains(provider))
            return Result.FromException<AssetHistoricalPrice>(
                new Exception(
                    "Provider not found. Allowed providers: "
                        + asset.Providers.Aggregate("", (str, prov) => $"{str}{prov.Name} ")
                )
            );

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
