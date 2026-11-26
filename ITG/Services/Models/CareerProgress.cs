namespace ITG.Services.Models;

// Status of one certificate inside the employee's roadmap
public enum RoadmapItemStatus
{
    NotSubmitted = 0,
    Pending = 1,
    Verified = 2,
    Rejected = 3
}

// One certificate in the roadmap, with the employee's current status for it
public record RoadmapItem(
    int CertificateId,
    string Title,
    string? Description,
    string Category,
    int SortOrder,
    RoadmapItemStatus Status,
    int? SubmissionId,
    DateTime? SubmittedAt,
    DateTime? IssuedOn,
    string? RejectionReason)
{
    public bool IsCompleted => Status == RoadmapItemStatus.Verified;
}

// Overall situation of an employee, used by the admin dashboard
public enum CareerStatus
{
    NotStarted = 0,
    InProgress = 1,
    ReviewPending = 2,
    ReadyForPromotion = 3,
    ChoosingPath = 4,          // HR opened several paths, the employee has to choose
    TopOfPath = 5,
    NoPosition = 6,
    AwaitingPathDecision = 7   // at a fork, waiting for HR to decide the path
}

// A path the employee can choose when standing at a fork
public record PathOption(int CareerPathId, string PathName, int PositionId, string PositionTitle);

// Everything the pages need to know about an employee's career progress
public class CareerProgress
{
    public int? CurrentPositionId { get; init; }
    public string? CurrentPositionTitle { get; init; }
    public string? CurrentPathName { get; init; }

    // Fork: HR decides the next path (or opens several for the employee to choose)
    public bool IsAtFork { get; init; }

    // Every path possible after this fork (what HR chooses from)
    public IReadOnlyList<PathOption> AllPathOptions { get; init; } = Array.Empty<PathOption>();

    // The paths HR opened for the employee
    public IReadOnlyList<PathOption> PathOptions { get; init; } = Array.Empty<PathOption>();
    public int? SelectedCareerPathId { get; init; }

    // The position the employee is working towards
    public int? TargetPositionId { get; init; }
    public string? TargetPositionTitle { get; init; }

    // Ordered certificates required for the target position
    public IReadOnlyList<RoadmapItem> Items { get; init; } = Array.Empty<RoadmapItem>();

    public bool HasPosition => CurrentPositionId != null;
    public bool HasTarget => TargetPositionId != null;
    public bool NeedsPathSelection => IsAtFork && !HasTarget;

    // HR opened paths: the employee picks one
    public bool CanChoosePath => NeedsPathSelection && PathOptions.Count > 0;

    // Nothing decided yet: waiting for HR
    public bool IsAwaitingPathDecision => NeedsPathSelection && PathOptions.Count == 0;
    public bool IsTopOfPath => HasPosition && !IsAtFork && !HasTarget;

    public int TotalCount => Items.Count;
    public int CompletedCount => Items.Count(i => i.IsCompleted);
    public int PendingCount => Items.Count(i => i.Status == RoadmapItemStatus.Pending);
    public int Percent => TotalCount == 0 ? 0 : (int)Math.Round(CompletedCount * 100.0 / TotalCount);

    // All required certificates verified: waiting for the admin to promote
    public bool IsReadyForPromotion => TotalCount > 0 && CompletedCount == TotalCount;

    // First certificate that is not completed yet
    public RoadmapItem? NextStep => Items.FirstOrDefault(i => !i.IsCompleted);

    public CareerStatus Status =>
        !HasPosition ? CareerStatus.NoPosition
        : IsAwaitingPathDecision ? CareerStatus.AwaitingPathDecision
        : CanChoosePath ? CareerStatus.ChoosingPath
        : IsTopOfPath ? CareerStatus.TopOfPath
        : IsReadyForPromotion ? CareerStatus.ReadyForPromotion
        : PendingCount > 0 ? CareerStatus.ReviewPending
        : CompletedCount == 0 ? CareerStatus.NotStarted
        : CareerStatus.InProgress;
}
