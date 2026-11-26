namespace ITG.Services.Models;

// Turns the soft-skills score into a rating label (translation key)
public static class SkillRating
{
    public static string LabelFor(int overallScore) => overallScore switch
    {
        >= 85 => "Excellent",
        >= 70 => "Strong Performer",
        >= 50 => "Developing",
        _ => "Needs Improvement"
    };
}
