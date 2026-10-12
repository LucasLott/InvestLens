using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using InvestLens.Application.Interfaces.Providers;
using InvestLens.Application.Models;
using InvestLens.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InvestLens.Infrastructure.Bolsai;

public sealed partial class BolsaiClient(HttpClient httpClient, IOptions<BolsaiSettings> settings, ILogger<BolsaiClient> logger, IApiRequestBudget? budget = null) : ICompanyMarketDataProvider
{
    private readonly HttpClient _httpClient = httpClient; private readonly BolsaiSettings _settings = settings.Value; private readonly ILogger<BolsaiClient> _logger = logger;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public async Task VerifyConnectivityAsync(CancellationToken ct) => _ = await GetAsync<Stocks>("stocks/?bdi_code=02&limit=1", ct);
    public async Task<CompanyMarketSnapshot> GetCompanySnapshotAsync(string ticker, CancellationToken ct)
    {
        var t = Normalize(ticker); var company = await GetAsync<Company>($"companies/{t}", ct); var quote = await GetAsync<Quote>($"stocks/{t}/quote", ct); var fund = await GetAsync<Fundamentals>($"fundamentals/{t}", ct);
        IReadOnlyList<DividendPayment> payments = [];
        try { payments = (await GetAsync<Dividends>($"dividends/{t}?years=5", ct)).Payments?.Select(p => new DividendPayment(p.ExDate,p.PaymentDate,p.Type,p.ValuePerShare)).ToArray() ?? []; }
        catch (BolsaiRequestException e) when (e.Kind is BolsaiFailureKind.Forbidden or BolsaiFailureKind.NotFound) { _logger.LogInformation("Dividendos indisponíveis para {Ticker}; categoria {FailureKind}.",t,e.Kind); }
        _logger.LogInformation("Consulta bolsai concluída para {Ticker}.",t);
        return new(t,company.TickerPrimary,company.CorporateName,company.TradeName,company.Sector,quote.TradeDate,quote.Close,fund.ReferenceDate,fund.Pl,fund.DividendYield,fund.Roe,fund.DebtEquity,fund.NetMargin,payments);
    }
    private async Task<T> GetAsync<T>(string path, CancellationToken ct)
    {
        if (budget is not null) await budget.ReserveAsync(ct); using var request=new HttpRequestMessage(HttpMethod.Get,path); request.Headers.TryAddWithoutValidation("X-API-Key",_settings.ApiKey);
        try { using var response=await _httpClient.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,ct); if(!response.IsSuccessStatusCode) throw HttpError(response.StatusCode); return await response.Content.ReadFromJsonAsync<T>(JsonOptions,ct) ?? throw Error(BolsaiFailureKind.InvalidResponse,"A bolsai retornou um corpo vazio ou inválido.",null,null); }
        catch(JsonException e){throw Error(BolsaiFailureKind.InvalidResponse,"A bolsai retornou JSON inválido.",null,e);} catch(OperationCanceledException) when(ct.IsCancellationRequested){throw;} catch(OperationCanceledException e){throw Error(BolsaiFailureKind.Timeout,"A chamada à bolsai excedeu o tempo limite configurado.",null,e);} catch(HttpRequestException e){throw Error(BolsaiFailureKind.NetworkUnavailable,"Não foi possível estabelecer comunicação com a bolsai.",null,e);}
    }
    private BolsaiRequestException HttpError(HttpStatusCode s) { var x=s switch { HttpStatusCode.Unauthorized=>(BolsaiFailureKind.Unauthorized,"A bolsai recusou a API Key informada."),HttpStatusCode.Forbidden=>(BolsaiFailureKind.Forbidden,"A API Key não possui permissão para este recurso da bolsai."),HttpStatusCode.NotFound=>(BolsaiFailureKind.NotFound,"O recurso solicitado não foi encontrado na bolsai."),HttpStatusCode.TooManyRequests=>(BolsaiFailureKind.RateLimited,"O limite de requisições da bolsai foi atingido."),_ when (int)s>=500=>(BolsaiFailureKind.ExternalServiceFailure,"A bolsai apresentou uma falha externa."),_=>(BolsaiFailureKind.ExternalServiceFailure,"A bolsai retornou uma resposta não esperada.")}; return Error(x.Item1,x.Item2,s,null); }
    private BolsaiRequestException Error(BolsaiFailureKind kind,string msg,HttpStatusCode? status,Exception? e) { _logger.LogWarning("Falha bolsai: {FailureKind}; status {StatusCode}; exceção {ExceptionType}.",kind,status is null?null:(int)status,e?.GetType().Name); return new(kind,msg,status,e); }
    private static string Normalize(string ticker) { var t=ticker?.Trim().ToUpperInvariant()??""; if(!Ticker().IsMatch(t)) throw new ArgumentException("Ticker inválido.",nameof(ticker)); return t; }
    [GeneratedRegex("^[A-Z]{4}[0-9]{1,2}$",RegexOptions.CultureInvariant)] private static partial Regex Ticker();
    private sealed class Stocks { public int Count {get;set;} }
    private sealed class Company { [JsonPropertyName("ticker_primary")]public string? TickerPrimary{get;set;} [JsonPropertyName("corporate_name")]public string? CorporateName{get;set;} [JsonPropertyName("trade_name")]public string? TradeName{get;set;} public string? Sector{get;set;} }
    private sealed class Quote { [JsonPropertyName("trade_date")]public DateOnly? TradeDate{get;set;} public decimal? Close{get;set;} }
    private sealed class Fundamentals { [JsonPropertyName("reference_date")]public DateOnly? ReferenceDate{get;set;} [JsonPropertyName("pl")]public decimal? Pl{get;set;} [JsonPropertyName("dividend_yield")]public decimal? DividendYield{get;set;} [JsonPropertyName("roe")]public decimal? Roe{get;set;} [JsonPropertyName("debt_equity")]public decimal? DebtEquity{get;set;} [JsonPropertyName("net_margin")]public decimal? NetMargin{get;set;} }
    private sealed class Dividends { public List<Dividend>? Payments {get;set;} } private sealed class Dividend { [JsonPropertyName("ex_date")]public DateOnly? ExDate{get;set;} [JsonPropertyName("payment_date")]public DateOnly? PaymentDate{get;set;} public string? Type{get;set;} [JsonPropertyName("value_per_share")]public decimal? ValuePerShare{get;set;} }
}
