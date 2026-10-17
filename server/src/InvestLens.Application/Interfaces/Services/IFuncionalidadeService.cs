namespace InvestLens.Application.Interfaces.Services
{
    public interface IFuncionalidadeService
    {
        Task<bool> PossuiPermissaoAsync(int idUsuario, string chave, CancellationToken cancellationToken);
        Task<IReadOnlyList<InvestLens.Application.DTOs.Funcionalidade.FuncionalidadeResponse>> ListarAsync(int? idUsuario, CancellationToken cancellationToken);
    }
}
