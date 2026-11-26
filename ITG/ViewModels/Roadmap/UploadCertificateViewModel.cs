namespace ITG.ViewModels.Roadmap;

// Data posted by the "Upload certificate" form
public class UploadCertificateViewModel
{
    public int CertificateId { get; set; }
    public IFormFile? File { get; set; }
    public DateTime? IssuedOn { get; set; }
}
