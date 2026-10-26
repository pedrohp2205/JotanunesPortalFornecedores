namespace Jotanunes.Infra.DocumentAi.Settings;

public class DocumentImageSettings
{
    public const string SectionName = "DocumentImages";

    public int MaxPages { get; set; } = 4;
    public int Dpi { get; set; } = 150;
}
