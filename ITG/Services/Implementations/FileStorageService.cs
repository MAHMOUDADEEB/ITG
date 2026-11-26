using ITG.Constants;
using ITG.Services.Interfaces;

namespace ITG.Services.Implementations;

// Stores certificates in a private folder (outside wwwroot),
// so they can only be opened through an authorized controller.
public class FileStorageService : IFileStorageService
{
    private readonly string _folder;

    public FileStorageService(IWebHostEnvironment environment)
    {
        _folder = Path.Combine(environment.ContentRootPath, AppConstants.CertificateStorageFolder);
        Directory.CreateDirectory(_folder);
    }

    public async Task<string> SaveCertificateAsync(IFormFile file, string extension)
    {
        // A random name: never trust or reuse the name sent by the browser
        var storedName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(_folder, storedName);

        await using var stream = new FileStream(fullPath, FileMode.CreateNew);
        await file.CopyToAsync(stream);

        return storedName;
    }

    public Stream? OpenCertificate(string storedFileName)
    {
        // GetFileName removes any folder parts, preventing "../" path tricks
        var safeName = Path.GetFileName(storedFileName);
        var fullPath = Path.Combine(_folder, safeName);

        return File.Exists(fullPath)
            ? new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read)
            : null;
    }
}
