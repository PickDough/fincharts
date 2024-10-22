using System;
using app_core.service;

namespace app_core.dto;

public record AssetRealtimePrice(
    Asset Asset,
    RealtimePriceKind Kind,
    double Price,
    int Volume,
    DateTimeOffset Timestamp
);
