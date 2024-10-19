using app_core.domain;
using app_core.repository;
using AutoMapper;
using db_context.entity;
using Microsoft.EntityFrameworkCore;

namespace db_context.repository;

public class AssetRepository(AssetsFetcherContext context, IMapper mapper) : IAssetRepository
{
    public async Task SaveAsync(IAsyncEnumerable<IEnumerable<Asset>> assets)
    {
        await foreach (var asset in assets)
        {
            await context
                .Assets.UpsertRange(mapper.Map<IEnumerable<AssetEntity>>(asset))
                .RunAsync();
        }
    }
}
