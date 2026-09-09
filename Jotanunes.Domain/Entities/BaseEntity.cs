namespace Jotanunes.Domain.Entities;

public abstract class BaseEntity
{
    public long Id { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
