namespace ITG.Models.Entities;

public class CareerPath
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Self-reference: a path can branch from a parent path (fork)
    public int? ParentCareerPathId { get; set; }
    public CareerPath? ParentCareerPath { get; set; }
    public ICollection<CareerPath> ChildCareerPaths { get; set; } = new List<CareerPath>();

    public ICollection<Position> Positions { get; set; } = new List<Position>();
}
