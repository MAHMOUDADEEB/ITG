namespace ITG.Services.Interfaces;

public interface IFileStorageService
{
    // Saves the file in the private certificates folder and returns the stored file name
    Task<string> SaveCertificateAsync(IFormFile file, string extension);

    // Opens a stored certificate, or returns null if it does not exist
    Stream? OpenCertificate(string storedFileName);
}
