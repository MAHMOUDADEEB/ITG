namespace ITG.Services.Models;

// An uploaded certificate file ready to be sent to the browser
public record CertificateFile(Stream Content, string ContentType, string FileName);
