namespace ITG.Models.Entities;

// History of promotions approved by an admin
public class Promotion
{
    public int Id { get; set; }

    public string EmployeeId { get; set; } = string.Empty;
    public Employee Employee { get; set; } = null!;

    public int? FromPositionId { get; set; }
    public Position? FromPosition { get; set; }

    public int ToPositionId { get; set; }
    public Position ToPosition { get; set; } = null!;

    public string? Note { get; set; }
    public DateTime PromotedAt { get; set; } = DateTime.UtcNow;

    public string? PromotedById { get; set; }
    public Employee? PromotedBy { get; set; }
}
