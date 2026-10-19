namespace FoodSupply.Api.Events;

public sealed record OrderPlacedEvent(
    Guid EventId,
    int OrderId,
    string OrderNumber,
    Guid CustomerId,
    int RestaurantId,
    decimal TotalAmount,
    string CurrencyCode,
    DateTime OccurredAtUtc);
