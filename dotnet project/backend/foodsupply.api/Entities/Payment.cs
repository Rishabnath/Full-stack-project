namespace FoodSupply.Api.Entities;

public sealed class Payment
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public PaymentMethod Method { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public decimal Amount { get; set; }

    public string CurrencyCode { get; set; } = "INR";

    public string? ProviderReference { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? PaidAtUtc { get; set; }

    public Order Order { get; set; } = null!;
}
