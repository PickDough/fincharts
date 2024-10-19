using app_core.domain;

namespace app_core.repository;

public interface IAssetRepository
{
    Task SaveAsync(IAsyncEnumerable<IEnumerable<Asset>> assets);
}
