using Microsoft.Extensions.Options;

namespace InvestLens.Infrastructure.Configuration
{
    public sealed class JwtSettingsValidator : IValidateOptions<JwtSettings>
    {
        public ValidateOptionsResult Validate(string? name, JwtSettings options)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(options.Issuer))
                errors.Add("Authentication:Jwt:Issuer é obrigatório.");

            if (string.IsNullOrWhiteSpace(options.Audience))
                errors.Add("Authentication:Jwt:Audience é obrigatório.");

            if (options.ExpirationInMinutes <= 0)
                errors.Add("Authentication:Jwt:ExpirationInMinutes deve ser maior que zero.");

            try 
            {
                options.CreateSigningKey();
            }
            catch (OptionsValidationException exception)
            {
                errors.AddRange(exception.Failures);
            }
            
            return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
        }
    }
}
