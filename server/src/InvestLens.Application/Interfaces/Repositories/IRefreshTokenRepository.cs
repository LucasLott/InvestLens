namespace InvestLens.Application.Interfaces.Repositories
{
    public interface IRefreshTokenRepository
    {
        Task CreateAsync(int userId, string hash, DateTime expiresAt, CancellationToken cancellationToken);
        // Rotation and replay-triggered family revocation must be committed atomically.
        Task<RefreshSession?> RotateAsync(string hash, string replacementHash, CancellationToken cancellationToken);
        Task RevokeAsync(string hash, CancellationToken cancellationToken);
    }

    public sealed class RefreshSession
    {
        public int IdUsuario { get; init; }
        public string Codigo { get; init; } = string.Empty;
        public string Nome { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public DateTime Expiracao { get; init; }
    }
}