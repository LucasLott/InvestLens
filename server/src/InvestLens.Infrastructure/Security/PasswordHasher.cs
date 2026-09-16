using System.Security.Cryptography;
using System.Text;
using InvestLens.Application.Interfaces.Security;
using Konscious.Security.Cryptography;

namespace InvestLens.Infrastructure.Security
{
    public class PasswordHasher : IPasswordHasher
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;

        private const int MemorySize = 19456;
        private const int Iterations = 2;
        private const int DegreeOfParallelism = 1;

        public string Hash(string password)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(password);

            var salt = RandomNumberGenerator.GetBytes(SaltSize);

            var hash = GenerateHash(password,
                                    salt,
                                    MemorySize,
                                    Iterations,
                                    DegreeOfParallelism,
                                    HashSize);

            return string.Concat("$argon2id",
                                 "$v=19",
                                 $"$m={MemorySize},t={Iterations},p={DegreeOfParallelism}",
                                 $"${Convert.ToBase64String(salt)}",
                                 $"${Convert.ToBase64String(hash)}");
        }

        public bool Verify(string password,
                           string passwordHash)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(password);
            ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

            try
            {
                var parts = passwordHash.Split('$', StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length != 5)
                    return false;

                if (!string.Equals(parts[0], "argon2id", StringComparison.Ordinal))
                    return false;

                if (!string.Equals(parts[1], "v=19", StringComparison.Ordinal))
                    return false;

                var parameters = ParseParameters(parts[2]);

                var salt = Convert.FromBase64String(parts[3]);
                var expectedHash = Convert.FromBase64String(parts[4]);

                var actualHash = GenerateHash(password,
                                              salt,
                                              parameters.MemorySize,
                                              parameters.Iterations,
                                              parameters.DegreeOfParallelism,
                                              expectedHash.Length);

                return CryptographicOperations.FixedTimeEquals(actualHash,
                                                               expectedHash);
            }
            catch (FormatException)
            {
                return false;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        private static byte[] GenerateHash(string password,
                                           byte[] salt,
                                           int memorySize,
                                           int iterations,
                                           int degreeOfParallelism,
                                           int hashSize)
        {
            using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password));

            argon2.Salt = salt;
            argon2.MemorySize = memorySize;
            argon2.Iterations = iterations;
            argon2.DegreeOfParallelism = degreeOfParallelism;

            return argon2.GetBytes(hashSize);
        }

        private static Argon2Parameters ParseParameters(string parameters)
        {
            var values = parameters.Split(',');

            if (values.Length != 3)
                throw new FormatException("Parâmetros Argon2id inválidos.");

            var memorySize = ParseParameter(values[0],
                                            "m");

            var iterations = ParseParameter(values[1],
                                            "t");

            var degreeOfParallelism = ParseParameter(values[2],
                                                     "p");

            if (memorySize <= 0 || iterations <= 0 || degreeOfParallelism <= 0)
                throw new FormatException("Parâmetros Argon2id inválidos.");

            return new Argon2Parameters(memorySize,
                                        iterations,
                                        degreeOfParallelism);
        }

        private static int ParseParameter(string parameter,
                                          string expectedName)
        {
            var parts = parameter.Split('=');

            if (parts.Length != 2 || !string.Equals(parts[0], expectedName, StringComparison.Ordinal) || !int.TryParse(parts[1], out var value))
                throw new FormatException("Parâmetro Argon2id inválido.");

            return value;
        }

        private sealed record Argon2Parameters(int MemorySize,
                                               int Iterations,
                                               int DegreeOfParallelism);
    }
}