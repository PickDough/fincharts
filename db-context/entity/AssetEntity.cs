namespace db_context.entity;

public record AssetEntity(
    string Id,
    string Symbol,
    string Kind,
    string Description,
    string Currency,
    string? BaseCurrency
);
