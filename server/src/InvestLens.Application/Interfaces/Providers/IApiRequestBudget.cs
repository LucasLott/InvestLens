namespace InvestLens.Application.Interfaces.Providers;
public interface IApiRequestBudget { Task ReserveAsync(CancellationToken cancellationToken); }
