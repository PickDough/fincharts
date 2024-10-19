using app_core.domain;
using DotNext;

namespace financial_data_provider;

public interface IAssetProvider
{
    IAsyncEnumerable<Result<IEnumerable<Asset>>> GetAssets();
}
