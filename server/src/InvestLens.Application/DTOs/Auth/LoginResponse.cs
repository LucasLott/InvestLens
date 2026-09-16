namespace InvestLens.Application.DTOs.Auth
{
    public sealed class LoginResponse
    {
        public int IdUsuario { get; init; }
        public string Codigo { get; init; } = string.Empty;
        public string Nome { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
    }
}
