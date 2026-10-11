using Microsoft.Extensions.Options;

namespace InvestLens.Infrastructure.Configuration
{
    public sealed class BolsaiSettingsValidator : IValidateOptions<BolsaiSettings>
    {
        public ValidateOptionsResult Validate(string? name, BolsaiSettings options)
        {
            var errors = new List<string>();

            if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUri) || baseUri.Scheme != Uri.UriSchemeHttps)
                errors.Add("Bolsai:BaseUrl deve ser uma URL HTTPS absoluta.");

            if (string.IsNullOrWhiteSpace(options.ApiKey))
                errors.Add("Bolsai:ApiKey é obrigatória. Configure-a em User Secrets ou em outro provedor seguro.");

            if (options.TimeoutSeconds <= 0)
                errors.Add("Bolsai:TimeoutSeconds deve ser maior que zero.");

            return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
        }
    }
}
