namespace FoodSupply.Api.Entities;

public sealed class CustomerAddress
{
    public int Id { get; set; }

    public Guid UserId { get; set; }

    public string Label { get; set; } = "Home";

    public string Line1 { get; set; } = string.Empty;

    public string? Line2 { get; set; }

    public string City { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public string PostalCode { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public bool IsDefault { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
}
