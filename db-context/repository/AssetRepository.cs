using app_core.dto;
using app_core.repository;
using AutoMapper;
using db_context.entity;
using DotNext;
using DotNext.Runtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace db_context.repository;

public class AssetRepository(AssetsFetcherContext context, IMapper mapper) : IAssetRepository
{
    public async Task<Result<Asset>> GetAsset(Guid assetId)
    {
        var asset = await context
            .Assets.Include(a => a.Providers)
            .FirstOrDefaultAsync(a => a.Id == assetId);

        if (asset == null)
        {
            return Result.FromException<Asset>(new Exception("Asset not found"));
        }
        return Result.FromValue(mapper.Map<Asset>(asset));
    }

    public async Task<(int totalPages, IEnumerable<Asset> assets)> GetAssetsPaginated(
        int PerPage,
        int Page
    )
    {
        var count = await context.Assets.CountAsync();
        var totalPages = (int)Math.Ceiling((double)count / PerPage);
        if (totalPages < Page)
        {
            return (totalPages, Enumerable.Empty<Asset>());
        }
        return (
            totalPages,
            (
                await context
                    .Assets.Include(a => a.Providers)
                    .OrderBy(a => a.Symbol)
                    .Skip((Page - 1) * PerPage)
                    .Take(PerPage)
                    .ToListAsync()
            ).Select(mapper.Map<Asset>)
        );
    }

    public async Task SaveAsync(IAsyncEnumerable<IEnumerable<Asset>> assetsStream)
    {
        await foreach (var assets in assetsStream)
        {
            foreach (var asset in assets)
            {
                await context.Database.ExecuteSqlInterpolatedAsync(
                    $@"INSERT INTO ""Assets"" VALUES ({asset.Id}, {asset.Symbol}, {asset.Kind}, {asset.Description}, {asset.Currency}, {asset.BaseCurrency ?? ""}) ON CONFLICT DO NOTHING;"
                );

                foreach (var provider in asset.Providers)
                {
                    await context.Database.ExecuteSqlAsync(
                        $@"INSERT INTO ""Providers"" VALUES ({provider.Name}) ON CONFLICT DO NOTHING;"
                    );

                    await context.Database.ExecuteSqlAsync(
                        $@"INSERT INTO ""AssetEntityProviderEntity"" VALUES ({asset.Id}, {provider.Name}) ON CONFLICT DO NOTHING;"
                    );
                }
            }
        }
    }
}
