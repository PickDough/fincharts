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
    public Dictionary<string, object> Mappings { get; set; }
}

internal struct CountBack
{
    public List<Bar> Data { get; set; }
}

internal struct Bar
{
    public DateTime T { get; set; }
    public double O { get; set; }
    public double H { get; set; }
    public double L { get; set; }
    public double C { get; set; }
    public int V { get; set; }
}
