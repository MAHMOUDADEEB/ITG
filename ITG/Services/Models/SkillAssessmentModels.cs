namespace ITG.Services.Models;

// The four soft skills measured by the assessment (also used as translation keys)
public static class SoftSkills
{
    public const string Teamwork = "Teamwork";
    public const string ProblemSolving = "Problem Solving";
    public const string Communication = "Communication";
    public const string Leadership = "Leadership";

    public static readonly string[] All = { Teamwork, ProblemSolving, Communication, Leadership };
}

// One option of a scenario question. Points (0-3) stay on the server.
public record AssessmentOption(string Text, int Points);

public record AssessmentQuestion(string Skill, string Scenario, List<AssessmentOption> Options);

// Questions generated for an employee, kept on the server until they submit
public record AssessmentDraft(List<AssessmentQuestion> Questions, DateTime CreatedAt);

// What the browser receives (no points)
public record AssessmentQuestionView(string Skill, string Scenario, IReadOnlyList<string> Options);

// Saved in SkillAssessment.DetailsJson so the admin can review the answers
public record AssessmentAnswerDetail(string Skill, string Scenario, string ChosenOption, int Points);
