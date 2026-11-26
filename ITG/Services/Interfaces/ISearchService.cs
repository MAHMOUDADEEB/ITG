using ITG.Services.Models;

namespace ITG.Services.Interfaces;

public interface ISearchService
{
    // Searches certificates and exams by title, description and category
    Task<SearchResults> SearchAsync(string employeeId, string query);
}
