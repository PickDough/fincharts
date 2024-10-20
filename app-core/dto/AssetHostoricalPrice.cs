using System;
using app_core.dto;

namespace app_core.dto;

public record AssetHistoricalPrice(Asset Asset, IEnumerable<OhlcBar> OhlcBars);
