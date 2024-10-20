namespace db_context.entity;

public record AssetEntity(
    Guid Id,
    string Symbol,
    string Kind,
    string Description,
    string Currency,
    string? BaseCurrency
)
{
    public static string TableName => "Assets";

    public List<ProviderEntity> Providers { get; set; } = [];
}
