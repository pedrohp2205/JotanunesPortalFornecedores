namespace Jotanunes.Infra.DocumentAi.Settings;

public class OpenRouterSettings
{
    public const string SectionName = "OpenRouter";

    public bool Enabled { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "google/gemini-3.8-flash";
    public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1/";
    public int TimeoutSeconds { get; set; } = 120;
}
