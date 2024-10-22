using System.ComponentModel.DataAnnotations;
using app_core.dto;
using app_core.repository;
using app_core.service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace api.controllers;

[Route("api/[controller]")]
[ApiController]
public class AssetsController : ControllerBase
{
    /// <summary>
    /// Get paginated assets.
    /// </summary>
    /// <param name="assetService">The asset repository.</param>
    /// <param name="perPage">The number of assets per page. Defaults to 100.</param>
    /// <param name="page">The page number. Defaults to 1.</param>
    [HttpGet]
    [ProducesResponseType<IEnumerable<Asset>>(200)]
    [ProducesErrorResponseType(typeof(string))]
    public async Task<IActionResult> GetAssets(
        [FromServices] AssetService assetService,
        [FromQuery] int perPage = 100,
        [FromQuery] int page = 1
    )
    {
        var assetsResult = await assetService.GetAssetsPaginated(perPage, page);
        if (!assetsResult.TryGet(out var assetsPaginated))
            return BadRequest($"Page {page} does not exist.");
        return Ok(
            new
            {
                Pagination = new { CurrentPage = page, TotalPages = assetsPaginated.totalPages },
                Assets = assetsPaginated.assets,
            }
        );
    }

    /// <summary>
    /// Get count of historical prices of an asset, counting back from the current time.
    /// </summary>
    /// <param name="id">The id of the asset to query.</param>
    /// <param name="provider">The provider of the asset to query.</param>
    /// <param name="interval">The time interval between each historical price. Must be between 1 and 100. Defaults to 1.</param>
    /// <param name="periodicity">The time periodicity of the historical prices. Defaults to Minute.</param>
    /// <param name="count">The number of historical prices to return. Must be between 1 and 100. Defaults to 10.</param>
    /// <returns>
    /// A JSON of <see cref="AssetHistoricalPrice"/> objects in reverse chronological order.
    /// </returns>
    /// <remarks>
    /// The server will time out if it takes longer than 5 seconds to return the response.
    ///
    /// If the asset is not supported by the provider, returns a 400 error.
    ///
    /// Example request:
    /// <code>
    ///     GET /api/assets/1234/kraken/count-back?interval=1&amp;periodicity=Minute&amp;count=10
    /// </code>
    /// </remarks>
    [HttpGet("{id}/{provider}/count-back")]
    [ProducesResponseType<AssetHistoricalPrice>(200)]
    [ProducesErrorResponseType(typeof(string))]
    public async Task<IActionResult> GetAssetCountBack(
        [FromServices] PriceService priceService,
        [FromRoute] Guid id,
        [FromRoute] string provider,
        [Range(1, 100)] [FromQuery] int interval = 1,
        [FromQuery] DateInterval periodicity = DateInterval.Minute,
        [Range(1, 100)] [FromQuery] int count = 10
    )
    {
        var historicalPrices = await priceService.GetAssetHistoricalPrices(
            id,
            new Provider(provider),
            interval,
            periodicity,
            count
        );
        if (!historicalPrices.IsSuccessful)
        {
            return BadRequest(historicalPrices.Error.Message);
        }
        return Ok(historicalPrices.Value);
    }
}
