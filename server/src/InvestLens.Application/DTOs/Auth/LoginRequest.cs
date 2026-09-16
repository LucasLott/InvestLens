namespace InvestLens.Application.DTOs.Auth
{
    public sealed class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
    }
}
