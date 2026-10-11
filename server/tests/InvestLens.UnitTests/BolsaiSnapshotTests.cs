using System.Net;
using InvestLens.Infrastructure.Bolsai;
using InvestLens.Infrastructure.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace InvestLens.UnitTests;

public sealed class BolsaiSnapshotTests
{
    [Fact]
    public async Task GetCompanySnapshotAsync_MapsDocumentedFieldsAndPreservesPercentages()
    {
        var handler = new Handler(request => request.RequestUri!.AbsolutePath switch
        {
            "/api/v1/companies/PETR4" => Json("{\"ticker_primary\":\"PETR4\",\"corporate_name\":\"PETROBRAS\",\"trade_name\":\"Petrobras\",\"sector\":\"Petróleo\"}"),
            "/api/v1/stocks/PETR4/quote" => Json("{\"trade_date\":\"2026-03-21\",\"close\":45.67}"),
            "/api/v1/fundamentals/PETR4" => Json("{\"reference_date\":\"2025-12-31\",\"pl\":5.32,\"dividend_yield\":6.0,\"roe\":26.6,\"debt_equity\":0.92,\"net_margin\":22.23}"),
            "/api/v1/dividends/PETR4" => Json("{\"payments\":[{\"ex_date\":\"2026-03-14\",\"payment_date\":\"2026-04-20\",\"type\":\"JCP\",\"value_per_share\":0.2934}]}"), _ => new(HttpStatusCode.NotFound)
        });
        var client = new BolsaiClient(new HttpClient(handler){BaseAddress=new Uri("https://api.usebolsai.com/api/v1/")},Options.Create(new BolsaiSettings{ApiKey="test",BaseUrl="https://api.usebolsai.com/api/v1/",TimeoutSeconds=30}),NullLogger<BolsaiClient>.Instance);
        var value = await client.GetCompanySnapshotAsync("petr4",CancellationToken.None);
        Assert.Equal(6.0m,value.DividendYield); Assert.Equal(26.6m,value.ReturnOnEquity); Assert.Equal(0.92m,value.DebtToEquity); Assert.Equal(22.23m,value.NetMargin); Assert.Single(value.Dividends); Assert.Equal(45.67m,value.ClosePrice);
    }
    [Fact] public async Task GetCompanySnapshotAsync_PreservesMissingFieldsAsNull()
    { var h=new Handler(_=>Json("{}")); var c=new BolsaiClient(new HttpClient(h){BaseAddress=new Uri("https://api.usebolsai.com/api/v1/")},Options.Create(new BolsaiSettings{ApiKey="test"}),NullLogger<BolsaiClient>.Instance); var x=await c.GetCompanySnapshotAsync("VALE3",CancellationToken.None); Assert.Null(x.PriceToEarnings); Assert.Empty(x.Dividends); }
    private static HttpResponseMessage Json(string s)=>new(HttpStatusCode.OK){Content=new StringContent(s,System.Text.Encoding.UTF8,"application/json")};
    private sealed class Handler(Func<HttpRequestMessage,HttpResponseMessage> f):HttpMessageHandler { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken c)=>Task.FromResult(f(r)); }
}
