using ITG.Services.Models;

namespace ITG.Services.Interfaces;

public interface IAnalyticsService
{
    // departmentId = null: the whole company
    Task<AnalyticsData> GetAsync(int? departmentId);

    // Throws AiUnavailableException when the AI cannot answer
    Task<AnalyticsInsight> WriteInsightAsync(AnalyticsData data, string cultureName);
}
