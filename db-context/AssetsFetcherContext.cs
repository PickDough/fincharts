using System;
using db_context.entity;
using Microsoft.EntityFrameworkCore;

namespace db_context;

public class AssetsFetcherContext(DbContextOptions<AssetsFetcherContext> options)
    : DbContext(options)
{
    public DbSet<AssetEntity> Assets { get; set; }

    public DbSet<ProviderEntity> Providers { get; set; }
}
