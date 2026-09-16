using InvestLens.Application.DTOs.Auth;

namespace InvestLens.Application.Interfaces.Services.Auth
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    }
}
