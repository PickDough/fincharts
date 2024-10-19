using app_core.domain;
using app_core.repository;
using db_context;
using db_context.entity;
using db_context.repository;
using financial_data_provider;
using financial_data_provider.fintacharts;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Configuration.AddEnvironmentVariables(prefix: "DBCONTEXT_");
builder.Configuration.AddEnvironmentVariables(prefix: "FINTACHARTS_");

builder.Services.AddDbContextPool<AssetsFetcherContext>(options =>
    options.UseNpgsql(builder.Configuration.GetValue<string>("DATABASE_URL")!)
);
builder.Services.AddSingleton<IAssetRepository, AssetRepository>();

builder.Services.AddSingleton<FintaChartsProvider>(opts => new FintaChartsProvider(
    builder.Configuration.GetValue<string>("API_URL")!,
    builder.Configuration.GetValue<string>("USERNAME")!,
    builder.Configuration.GetValue<string>("PASSWORD")!
));

builder.Services.AddAutoMapper(cfg =>
{
    cfg.CreateMap<Asset, AssetEntity>();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();
