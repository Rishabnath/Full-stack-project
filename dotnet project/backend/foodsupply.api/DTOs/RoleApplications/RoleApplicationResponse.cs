using FoodSupply.Api.Entities;

namespace FoodSupply.Api.DTOs.RoleApplications;

public sealed class RoleApplicationResponse
{
    public Guid Id { get; set; }

    public Guid ApplicantUserId { get; set; }

    public string ApplicantName { get; set; } = string.Empty;

    public string ApplicantEmail { get; set; } = string.Empty;

    public UserRole RequestedRole { get; set; }

    public UserRoleApplicationStatus Status { get; set; }

    public string? BusinessName { get; set; }

    public string ApplicationDetails { get; set; } = string.Empty;

    public string? ReviewNote { get; set; }

    public DateTime SubmittedAtUtc { get; set; }

    public DateTime? ReviewedAtUtc { get; set; }
}
