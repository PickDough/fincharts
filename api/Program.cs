using System.Reflection;
using System.Text.Json.Serialization;
using app_core.dto;
using app_core.repository;
using app_core.service;
using db_context;
using db_context.entity;
using db_context.repository;
using financial_data_provider.fintacharts;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

ConfigureApi(builder);
InjectAppDependencies(builder);
ConfigureMapping(builder);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AssetsFetcherContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.UseWebSockets();

app.Run();

static void InjectAppDependencies(WebApplicationBuilder builder)
{
    builder.Configuration.AddEnvironmentVariables(prefix: "DBCONTEXT_");
    builder.Configuration.AddEnvironmentVariables(prefix: "FINTACHARTS_");

    builder.Services.AddDbContextPool<AssetsFetcherContext>(options =>
        options.UseNpgsql(builder.Configuration.GetValue<string>("DATABASE_URL")!)
    );
    builder.Services.AddTransient<IAssetRepository, AssetRepository>();

    builder.Services.AddSingleton(opts => new FintaChartsProvider(
        builder.Configuration.GetValue<string>("API_URL")!,
        builder.Configuration.GetValue<string>("USERNAME")!,
        builder.Configuration.GetValue<string>("PASSWORD")!,
        opts.GetRequiredService<ILogger<FintaChartsProvider>>()
    ));
    builder.Services.AddSingleton<IAssetsHistoricalPriceProvider>(opts =>
        opts.GetService<FintaChartsProvider>()!
    );
    builder.Services.AddSingleton<IAssetRealtimePriceProvider>(opts =>
        opts.GetService<FintaChartsProvider>()!
    );
    builder.Services.AddTransient<AssetService>();
    builder.Services.AddTransient<PriceService>();
}

static void ConfigureApi(WebApplicationBuilder builder)
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(opt =>
    {
        var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        opt.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
    });
    builder
        .Services.AddControllers()
        .AddJsonOptions(opt =>
        {
            opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
    ;
    builder.Services.AddRouting(options => options.LowercaseUrls = true);
}

static void ConfigureMapping(WebApplicationBuilder builder)
{
    builder.Services.AddAutoMapper(cfg =>
    {
        cfg.CreateMap<AssetEntity, Asset>()
            .ConvertUsing(entity => new Asset(
                entity.Id,
                entity.Symbol,
                entity.Kind,
                entity.Description,
                entity.Currency,
                entity.BaseCurrency
            )
            {
                Providers = entity.Providers.Select(p => new Provider(p.Name)).ToList(),
            });
    });
}
