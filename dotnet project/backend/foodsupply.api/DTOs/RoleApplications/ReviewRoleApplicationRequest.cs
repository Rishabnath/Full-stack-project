using System.ComponentModel.DataAnnotations;

namespace FoodSupply.Api.DTOs.RoleApplications;

public sealed class ReviewRoleApplicationRequest
{
    [StringLength(500, ErrorMessage = "Review note cannot exceed 500 characters.")]
    public string? ReviewNote { get; set; }
}
