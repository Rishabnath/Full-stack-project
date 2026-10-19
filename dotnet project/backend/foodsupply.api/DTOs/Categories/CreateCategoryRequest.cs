using System.ComponentModel.DataAnnotations;

namespace FoodSupply.Api.DTOs.Categories;

public sealed class CreateCategoryRequest
{
    [Required(ErrorMessage = "Category name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Category name must be between 2 and 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(300, ErrorMessage = "Category description cannot exceed 300 characters.")]
    public string? Description { get; set; }
}
