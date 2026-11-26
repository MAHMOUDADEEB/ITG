using ITG.Constants;
using Microsoft.EntityFrameworkCore;

namespace ITG.Data;

public static class CategoryQueries
{
    // Category drop-down: the default categories plus any category already used
    public static async Task<List<string>> GetCategoryOptionsAsync(this AppDbContext context)
    {
        var used = await context.Certificates.Select(c => c.Category)
            .Union(context.Exams.Select(e => e.Category))
            .ToListAsync();

        return AppConstants.DefaultCategories
            .Concat(used)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c)
            .ToList();
    }
}
