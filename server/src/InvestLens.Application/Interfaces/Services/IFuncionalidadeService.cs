namespace InvestLens.Application.Interfaces.Services
{
    public interface IFuncionalidadeService
    {
        Task<bool> PossuiPermissaoAsync(int idUsuario, string chave, CancellationToken cancellationToken);
    }
}