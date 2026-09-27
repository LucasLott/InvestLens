namespace InvestLens.Application.Interfaces.Authentication
{
    public interface ICurrentUser
    {
        // Ausência de contexto autenticado ou claim inválida resulta em null.
        int? IdUsuario { get; }
        string? Codigo { get; }
        string? Nome { get; }
    }
}
