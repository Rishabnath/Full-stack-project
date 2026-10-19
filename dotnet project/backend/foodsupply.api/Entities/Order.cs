namespace FoodSupply.Api.Entities;

public sealed class Order
{
    public int Id { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }

    public int RestaurantId { get; set; }

    public Guid? DeliveryPartnerId { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Placed;

    public decimal Subtotal { get; set; }

    public decimal DeliveryFee { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public string CurrencyCode { get; set; } = "INR";

    public string RecipientName { get; set; } = string.Empty;

    public string RecipientPhoneNumber { get; set; } = string.Empty;

    public string DeliveryAddressLine1 { get; set; } = string.Empty;

    public string? DeliveryAddressLine2 { get; set; }

    public string DeliveryCity { get; set; } = string.Empty;

    public string DeliveryState { get; set; } = string.Empty;

    public string DeliveryPostalCode { get; set; } = string.Empty;

    public string DeliveryCountry { get; set; } = string.Empty;

    public string? CustomerNote { get; set; }

    public DateTime PlacedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ApplicationUser Customer { get; set; } = null!;

    public Restaurant Restaurant { get; set; } = null!;

    public ApplicationUser? DeliveryPartner { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
