namespace InvestLens.Application.Validators;

public static class CpfValidator
{
    public static bool IsValid(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf) || cpf.Length != 11 || cpf.Any(character => !char.IsAsciiDigit(character)))
            return false;

        if (cpf.All(character => character == cpf[0]))
            return false;

        return CalculateDigit(cpf, 9, 10) == cpf[9] - '0'
            && CalculateDigit(cpf, 10, 11) == cpf[10] - '0';
    }

    private static int CalculateDigit(string cpf, int length, int initialWeight)
    {
        var sum = 0;

        for (var index = 0; index < length; index++)
            sum += (cpf[index] - '0') * (initialWeight - index);

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
