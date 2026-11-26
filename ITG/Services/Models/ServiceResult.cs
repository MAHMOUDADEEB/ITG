namespace ITG.Services.Models;

// Result of an operation. ErrorKey is a translation key shown to the user.
public record ServiceResult(bool Succeeded, string? ErrorKey = null)
{
    public static ServiceResult Ok() => new(true);
    public static ServiceResult Fail(string errorKey) => new(false, errorKey);
}
