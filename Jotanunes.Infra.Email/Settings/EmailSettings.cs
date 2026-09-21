namespace Jotanunes.Infra.Email.Settings;

public class EmailSettings
{
    public const string SectionName = "Email";

    public bool Enabled { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string? FromName { get; set; }
    public string ApiUrl { get; set; } = "https://api.resend.com/emails";
}
