using System.ComponentModel.DataAnnotations;

namespace ITG.ViewModels.Admin;

// Forms used by the "Manage" pages. Id = 0 means "create", otherwise "edit".

public class CertificateForm
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Category is required.")]
    [StringLength(50)]
    public string Category { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public class CareerPathForm
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public int? ParentCareerPathId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class PositionForm
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [Range(1, 20, ErrorMessage = "Level must be between 1 and 20.")]
    public int Level { get; set; } = 1;

    [Range(1, int.MaxValue, ErrorMessage = "Please choose a career path.")]
    public int CareerPathId { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsFork { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ExamForm
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Category is required.")]
    [StringLength(50)]
    public string Category { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(5, 180, ErrorMessage = "Duration must be between 5 and 180 minutes.")]
    public int DurationMinutes { get; set; } = 30;

    [Range(5, 30, ErrorMessage = "Questions must be between 5 and 30.")]
    public int QuestionCount { get; set; } = 15;

    // Positions whose employees see this exam (current role or next role)
    public List<int> PositionIds { get; set; } = new();

    public bool IsActive { get; set; } = true;
}

public class DepartmentForm
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public class EmployeeForm
{
    // Empty = new employee
    public string? Id { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Employee ID is required.")]
    [StringLength(20)]
    public string EmployeeNumber { get; set; } = string.Empty;

    public int? DepartmentId { get; set; }
    public int? CurrentPositionId { get; set; }

    // Required only when creating an employee
    public string? Password { get; set; }
}

public class RoadmapItemForm
{
    public int PositionId { get; set; }
    public int CertificateId { get; set; }
}

// Simple id + text pair for drop-downs
public record LookupItem(int Id, string Text);

// One employee in the manage list
public record EmployeeListItem(
    string Id,
    string Name,
    string Email,
    string EmployeeNumber,
    int? DepartmentId,
    string? DepartmentName,
    int? CurrentPositionId,
    string? CurrentPositionTitle);

// ----- Rows for the Manage pages -----
public record CertificateListItem(int Id, string Title, string Category, string? Description, bool IsActive,
    int RoadmapCount, int UploadCount);

public record PositionListItem(int Id, string Title, int Level, string? Description, bool IsFork, bool IsActive,
    int CareerPathId, int EmployeeCount, int CertificateCount);

public record CareerPathListItem(int Id, string Name, string? Description, bool IsActive, int? ParentCareerPathId,
    string? ParentName, List<PositionListItem> Positions);

public record ExamListItem(int Id, string Title, string Category, string? Description, int DurationMinutes,
    int QuestionCount, List<int> PositionIds, List<string> PositionTitles, bool IsActive, int AttemptCount);

// A position in a drop-down, grouped by its career path
public record PositionLookupItem(int Id, string Title, string PathName);

public record DepartmentListItem(int Id, string Name, bool IsActive, int EmployeeCount);

public record RoadmapRow(int CertificateId, string Title, string Category, bool IsActive, int SortOrder);

public record RoadmapManageViewModel(
    int? PositionId,
    IReadOnlyList<LookupItem> Positions,
    IReadOnlyList<RoadmapRow> Items,
    IReadOnlyList<LookupItem> AvailableCertificates);

// Model of the Approve / Reject buttons
public record ReviewActionModel(int SubmissionId, string CertificateTitle);
