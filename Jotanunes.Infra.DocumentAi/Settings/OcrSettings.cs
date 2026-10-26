namespace Jotanunes.Infra.DocumentAi.Settings;

public class OcrSettings
{
    public const string SectionName = "Ocr";

    public bool Enabled { get; set; } = true;
    public string TesseractPath { get; set; } = "tesseract";
    public string Language { get; set; } = "por";
    public int PageSegmentationMode { get; set; } = 6;
    public int Dpi { get; set; } = 300;
    public int MaxPages { get; set; } = 6;
    public int TimeoutSeconds { get; set; } = 60;
}
