using Microsoft.EntityFrameworkCore;

namespace db_context.entity;

[PrimaryKey("Name")]
public record ProviderEntity(string Name)
{
    public static string TableName => "AssetProviders";

    public List<AssetEntity> Assets { get; set; } = [];
}
