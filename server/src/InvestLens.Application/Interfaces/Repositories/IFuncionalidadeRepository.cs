namespace InvestLens.Application.Interfaces.Repositories
{
    public interface IFuncionalidadeRepository
    {
        Task<bool> PossuiPermissaoAsync(string planoEfetivo, string chave, CancellationToken cancellationToken);
        Task<IReadOnlyList<(int Id, string Nome, string Pro)>> ListarAsync(CancellationToken cancellationToken);
    }
}
