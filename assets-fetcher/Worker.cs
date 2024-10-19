using app_core.repository;
using DotNext;
using financial_data_provider;

namespace assets_fetcher;

public class Worker(
    ILogger<Worker> logger,
    IAssetRepository assetRepository,
    IAssetProvider assetProvider
)
{
    public async Task ExecuteAsync()
    {
        logger.LogInformation("Worker started at: {time}", DateTimeOffset.Now);

        await assetRepository.SaveAsync(
            assetProvider
                .GetAssets()
                .Select(a =>
                    a.OrInvoke(() =>
                    {
                        logger.LogCritical("Failed to get assets: {}", a.Error!.Message);
                        return [];
                    })
                )
        );

        logger.LogInformation("Worker finished at: {time}", DateTimeOffset.Now);
    }
}
