using System.Data;
using Dapper;
using InvestLens.Application.Interfaces.Providers;
using InvestLens.Infrastructure.Configuration;
using InvestLens.Infrastructure.Database.Connection;
using InvestLens.Infrastructure.Database.Extensions;
using Microsoft.Extensions.Options;

namespace InvestLens.Infrastructure.Bolsai;
public sealed class BolsaiRequestBudget(IDbConnectionFactory factory, IOptions<BolsaiSettings> settings, TimeProvider clock) : IApiRequestBudget
{
  public async Task ReserveAsync(CancellationToken ct)
  {
    var p = new DynamicParameters(); var s = settings.Value;
    p.Add("@NM_Fonte", "bolsai", DbType.AnsiString, size:80); p.Add("@DT_Janela", DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime)); p.Add("@QT_Orcamento", s.DailyOperationalBudget);
    p.AdicionarReturnCodeErrMsg(); await using var c=factory.CreateConnection();
    try { await c.ExecuteAsync(new CommandDefinition("dbo.st_BolsaiReservarRequisicao",p,commandType:CommandType.StoredProcedure,cancellationToken:ct)); }
    catch { p.ValidarReturnCodeErrMsg(); throw; }
    p.ValidarReturnCodeErrMsg();
  }
}
