using InvestLens.Application.DTOs.Auth;

namespace InvestLens.Application.Interfaces.Security
{
    public interface IJwtTokenGenerator
    {
        Token Generate(int idUsuario, string codigo, string nome);
    }
}
