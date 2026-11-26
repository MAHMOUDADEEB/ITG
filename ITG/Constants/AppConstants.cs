namespace ITG.Constants;

public static class AppConstants
{
    // Minimum exam score (out of 100) counted as "Passed"
    public const int ExamPassingScore = 60;

    // Private folder (relative to the project root) where uploaded certificates are stored.
    // It is outside wwwroot, so files are only reachable through an authorized controller.
    public const string CertificateStorageFolder = "App_Data/certificates";

    public const string SampleCertificateFileName = "sample-certificate.pdf";

    // Upload rules for certificate files
    public const long MaxCertificateFileBytes = 5 * 1024 * 1024; // 5 MB
    public static readonly string[] AllowedCertificateExtensions = { ".pdf", ".png", ".jpg", ".jpeg" };

    // Categories offered in the admin drop-downs (certificates and exams)
    public static readonly string[] DefaultCategories = { "Technical", "Management", "Business & People" };
}
