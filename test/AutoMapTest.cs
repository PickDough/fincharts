using System;
using System.Security.Cryptography.X509Certificates;
using app_core.dto;
using AutoMapper;
using db_context.entity;

namespace test;

public class AutoMapTest
{
    private readonly IConfigurationProvider configuration;

    public AutoMapTest()
    {
        configuration = new MapperConfiguration(cfg =>
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

    [Fact]
    public void TestAutoMapperConfiguration()
    {
        configuration.AssertConfigurationIsValid();
    }
}
