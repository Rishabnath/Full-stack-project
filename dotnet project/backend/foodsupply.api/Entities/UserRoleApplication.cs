namespace FoodSupply.Api.Entities;

public sealed class UserRoleApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ApplicantUserId { get; set; }

    public UserRole RequestedRole { get; set; }

    public UserRoleApplicationStatus Status { get; set; } = UserRoleApplicationStatus.Pending;

    public string? BusinessName { get; set; }

    public string ApplicationDetails { get; set; } = string.Empty;

    public string? ReviewNote { get; set; }

    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;

    public Guid? ReviewedByUserId { get; set; }

    public DateTime? ReviewedAtUtc { get; set; }

    public ApplicationUser Applicant { get; set; } = null!;

    public ApplicationUser? Reviewer { get; set; }
}
