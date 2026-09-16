namespace InvestLens.Api.Configuration
{
    public sealed class SwaggerSettings
    {
        public const string SectionName = "Swagger";

        public string Title { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool EnableSwaggerProd { get; set; }
    }
}
