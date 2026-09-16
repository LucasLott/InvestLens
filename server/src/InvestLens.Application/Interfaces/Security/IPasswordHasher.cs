namespace InvestLens.Application.Interfaces.Security
{
    public interface IPasswordHasher
    {
        Task<string> Hash(string password);

        Task<bool> Verify(string password,
                          string passwordHash);
    }
}