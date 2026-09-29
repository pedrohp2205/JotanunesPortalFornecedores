namespace Jotanunes.Internal.BddTests.Support.Models;

public class PageListResponseDto<T>
{
    public List<T> Items { get; set; } = [];
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}
