using System.Text.Json.Serialization;

namespace InvestLens.Application.DTOs.Auth
{
    public sealed class LoginResponse
    {
        [JsonIgnore]
        public int IdUsuario { get; init; }
        
        [JsonIgnore]
        public string Codigo { get; init; } = string.Empty;
        public string Nome { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public Token Token { get; init; } = new Token();
    }

    public sealed class Token
    {
        public string AccessToken { get; init; } = string.Empty;
        public DateTime Expiration { get; init; }
    }
}
