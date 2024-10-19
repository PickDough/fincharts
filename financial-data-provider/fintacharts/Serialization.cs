using System;

namespace financial_data_provider.fintacharts;

internal struct ApiKeyResponse
{
    public string AccessToken { get; set; }
}

internal struct InstrumentsResponse
{
    public Paging Paging { get; set; }
    public List<Instrument> Data { get; set; }
}

internal struct Paging
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int Pages { get; set; }
}

internal struct Instrument
{
    public string Id { get; set; }
    public string Symbol { get; set; }
    public string Kind { get; set; }
    public string Description { get; set; }
    public string Currency { get; set; }
    public string? BaseCurrency { get; set; }
}
