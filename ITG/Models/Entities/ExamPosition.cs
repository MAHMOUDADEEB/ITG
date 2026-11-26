namespace ITG.Models.Entities;

// Join table: which positions an exam is meant for
public class ExamPosition
{
    public int ExamId { get; set; }
    public Exam Exam { get; set; } = null!;

    public int PositionId { get; set; }
    public Position Position { get; set; } = null!;
}
