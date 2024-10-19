using app_core.domain;
using app_core.repository;
using app_core.service;
using assets_fetcher;
using db_context;
using db_context.entity;
using db_context.repository;
using financial_data_provider.fintacharts;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<Worker>();
builder.Logging.AddConsole();

builder.Configuration.AddEnvironmentVariables(prefix: "DBCONTEXT_");
builder.Configuration.AddEnvironmentVariables(prefix: "FINTACHARTS_");

builder.Services.AddSingleton(
    (_) =>
        new AssetsFetcherContext(
            new DbContextOptionsBuilder<AssetsFetcherContext>()
                .UseNpgsql(builder.Configuration.GetConnectionString("AssetsFetcherContext")!)
                .Options
        )!
);

builder.Services.AddSingleton<IAssetRepository, AssetRepository>();
builder.Services.AddSingleton<IAssetProvider, FintaChartsProvider>(opts => new FintaChartsProvider(
    builder.Configuration.GetValue<string>("API_URL")!,
    builder.Configuration.GetValue<string>("USERNAME")!,
    builder.Configuration.GetValue<string>("PASSWORD")!
));

builder.Services.AddAutoMapper(cfg =>
{
    cfg.CreateMap<Asset, AssetEntity>();
});

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AssetsFetcherContext>();
    db.Database.Migrate();
}

host.Services.GetService<Worker>()!.ExecuteAsync().Wait();
