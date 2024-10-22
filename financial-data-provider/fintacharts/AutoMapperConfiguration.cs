using System;
using app_core.dto;
using AutoMapper;

namespace financial_data_provider.fintacharts;

internal static class AutoMapperConfiguration
{
    public static Mapper Mapper =>
        new(
            new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Instrument, Asset>()
                    .ForMember(
                        dest => dest.Providers,
                        opt =>
                            opt.MapFrom(src =>
                                src.Mappings.Keys.Select(m => new Provider(m)).ToList<Provider>()
                            )
                    );

                cfg.CreateMap<CountBack, IEnumerable<OhlcBar>>()
                    .ConvertUsing(cb =>
                        cb.Data.Select(b => new OhlcBar(b.O, b.H, b.L, b.C, b.V, b.T))
                    );
            })
        );
}
