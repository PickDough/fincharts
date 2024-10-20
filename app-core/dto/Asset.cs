using System;

namespace app_core.dto;

public record Asset(
    Guid Id,
    string Symbol,
    string Kind,
    string Description,
    string Currency,
    string? BaseCurrency
)
{
    public List<Provider> Providers { get; set; } = [];
}
