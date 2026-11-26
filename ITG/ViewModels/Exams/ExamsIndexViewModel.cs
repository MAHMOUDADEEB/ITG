using ITG.Services.Models;

namespace ITG.ViewModels.Exams;

public class ExamsIndexViewModel
{
    public ExamsOverview Overview { get; init; } = new();

    // "all", "available" or "completed"
    public string Filter { get; init; } = "all";

    public IEnumerable<ExamCard> VisibleExams => Filter switch
    {
        // Failed exams stay available until they are passed
        "available" => Overview.Exams.Where(e => !e.IsPassed),
        "completed" => Overview.Exams.Where(e => e.IsPassed),
        _ => Overview.Exams
    };
}
