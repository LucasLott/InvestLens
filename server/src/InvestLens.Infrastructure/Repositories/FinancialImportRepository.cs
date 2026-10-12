using System.Data;
using Dapper;
using InvestLens.Application.Interfaces.Repositories;
using InvestLens.Application.Models;
using InvestLens.Infrastructure.Database.Connection;
using InvestLens.Infrastructure.Database.Extensions;

namespace InvestLens.Infrastructure.Repositories;

public sealed class FinancialImportRepository(IDbConnectionFactory factory) : IFinancialImportRepository
{
    public async Task PersistAsync(CompanyMarketSnapshot s, CancellationToken cancellationToken)
    {
        var p = new DynamicParameters();
        p.Add("@CD_Ticker", s.QueriedTicker, DbType.AnsiString, size: 20);
        p.Add("@CD_TickerPrimario", s.PrimaryTicker, DbType.AnsiString, size: 20);
        p.Add("@NM_Empresa", s.TradeName, DbType.AnsiString, size: 80);
        p.Add("@NM_RazaoSocial", s.CorporateName, DbType.AnsiString, size: 255);
        p.Add("@DS_Setor", s.Sector, DbType.AnsiString, size: 255);
        p.Add("@NM_Fonte", "bolsai", DbType.AnsiString, size: 80);
        p.Add("@DT_Fundamentos", s.FundamentalsReferenceDate);
        p.Add("@VL_PL", s.PriceToEarnings);
        p.Add("@PE_DividendYield", s.DividendYield);
        p.Add("@PE_ROE", s.ReturnOnEquity);
        p.Add("@VL_DividaPatrimonio", s.DebtToEquity);
        p.Add("@PE_MargemLiquida", s.NetMargin);
        p.Add("@DT_Cotacao", s.QuoteDate);
        p.Add("@VL_PrecoFechamento", s.ClosePrice);
        p.AdicionarReturnCodeErrMsg();
        await using var connection = factory.CreateConnection();
        await connection.QueryFirstAsync(new CommandDefinition("dbo.st_ImportacaoFinanceiraUpsert", p, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));
        p.ValidarReturnCodeErrMsg();
    }
}
