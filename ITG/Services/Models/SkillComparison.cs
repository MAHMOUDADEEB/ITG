using ITG.Models.Entities;

namespace ITG.Services.Models;

// The latest soft-skills assessment and the one before it (null when there is only one)
public record SkillComparison(SkillAssessment Latest, SkillAssessment? Previous);
