namespace Jotanunes.Application.Settings;

public class NotificationSettings
{
    public const string SectionName = "Email";

    public string? PortalUrl { get; set; }

    public string ResetPasswordPath { get; set; } = "/redefinir-senha";
}
