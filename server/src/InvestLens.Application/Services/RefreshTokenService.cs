using System.Security.Cryptography;
using System.Text;

namespace InvestLens.Application.Services
{

    public sealed class RefreshTokenService(IRefreshTokenRepository repository, IJwtTokenGenerator jwt, TimeProvider clock)
    {
        public async Task<RefreshCookie> CreateAsync(int userId, CancellationToken cancellationToken)
        {
            var token = Generate();

            var expires = clock.GetUtcNow().UtcDateTime.AddDays(7);

            await repository.CreateAsync(userId, Hash(token), expires, cancellationToken);

            return new(token, expires);
        }

        public async Task<(LoginResponse Response, RefreshCookie Cookie)> RefreshAsync(string? token, CancellationToken cancellationToken)
        {
            if (!IsValid(token)) throw new CredenciaisInvalidasException();

            var replacement = Generate();

            var session = await repository.RotateAsync(Hash(token!), Hash(replacement), cancellationToken);

            if (session is null) throw new CredenciaisInvalidasException();

            return (new LoginResponse
            {
                IdUsuario = session.IdUsuario,
                Codigo = session.Codigo,
                Nome = session.Nome,
                Email = session.Email,
                Token = jwt.Generate(session.IdUsuario, session.Codigo, session.Nome)
            }, new(replacement, DateTime.SpecifyKind(session.Expiracao, DateTimeKind.Utc)));
        }

        public Task RevokeAsync(string? token, CancellationToken cancellationToken) => IsValid(token)
            ? repository.RevokeAsync(Hash(token!), cancellationToken) : Task.CompletedTask;

        private static bool IsValid(string? token) => token is { Length: 64 } && token.All(char.IsAsciiHexDigit);
        private static string Generate() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(token)));
    }

    // Internal transport value: never serialize or log the refresh secret.
    public sealed record RefreshCookie(string Value, DateTime ExpiresAt)
    {
        public override string ToString() => "[refresh cookie redacted]";
    }
}
