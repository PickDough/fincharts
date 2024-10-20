using System;

namespace app_core.dto;

public record OhlcBar(
    double Open,
    double High,
    double Low,
    double Close,
    int Volatility,
    DateTime Timestamp
);
