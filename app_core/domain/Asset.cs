using System;

namespace app_core.domain;

public record Asset(
    string Id,
    string Symbol,
    string Kind,
    string Description,
    string Currency,
    string? BaseCurrency
);
