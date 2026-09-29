namespace Jotanunes.External.BddTests.Support.Models;

public class ErrorResponseDto
{
    public string Message { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; }
}
