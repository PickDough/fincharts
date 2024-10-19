using app_core.domain;
using DotNext;

namespace app_core.service;

public interface IAssetProvider
{
    IAsyncEnumerable<Result<IEnumerable<Asset>>> GetAssets();
}
