using app_core.dto;
using DotNext;

namespace app_core.repository;

public interface IAssetRepository
{
    Task<Result<Asset>> GetAsset(Guid assetId);
    Task<(int totalPages, IEnumerable<Asset> assets)> GetAssetsPaginated(int PerPage, int Page);
    Task SaveAsync(IAsyncEnumerable<IEnumerable<Asset>> assets);
}
