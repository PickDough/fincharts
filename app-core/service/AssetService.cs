using System;
using System.Runtime.CompilerServices;
using app_core.dto;
using app_core.repository;
using DotNext;

namespace app_core.service;

public class AssetService(IAssetRepository assetRepository)
{
    public async Task<Result<(int totalPages, IEnumerable<Asset> assets)>> GetAssetsPaginated(
        int perPage,
        int page
    )
    {
        var (totalPages, assets) = await assetRepository.GetAssetsPaginated(perPage, page);
        if (totalPages < page)
        {
            return Result.FromException<(int, IEnumerable<Asset>)>(
                new Exception($"Page {page} does not exist. Total pages: {totalPages}")
            );
        }

        return Result.FromValue((totalPages, assets));
    }

    public async Task<Result<Asset>> GetAssetByIdAndProvider(Guid assetId, Provider provider)
    {
        var assetResult = await assetRepository.GetAsset(assetId);
        if (!assetResult.TryGet(out var asset))
            return Result.FromException<Asset>(new Exception("Failed to get asset"));

        if (!asset.Providers.Contains(provider))
            return Result.FromException<Asset>(
                new Exception(
                    "Provider not found. Allowed providers: "
                        + asset.Providers.Aggregate("", (str, prov) => $"{str}{prov.Name} ")
                )
            );

        return assetResult;
    }
}
