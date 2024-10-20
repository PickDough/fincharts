using app_core.dto;
using DotNext;

namespace app_core.service;

public interface IAssetProvider
{
    IAsyncEnumerable<Result<IEnumerable<Asset>>> GetAssets();
}
