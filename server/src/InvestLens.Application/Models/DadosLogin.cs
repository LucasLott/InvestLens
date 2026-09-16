namespace InvestLens.Application.Models
{
    // Modelo interno de autenticação; não deve ser utilizado como resposta HTTP.
    public sealed class DadosLogin
    {
        public int IdUsuario { get; init; }
        public string Codigo { get; init; } = string.Empty;
        public string Nome { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string SenhaHash { get; init; } = string.Empty;
        public bool Ativo { get; init; }
    }
}
