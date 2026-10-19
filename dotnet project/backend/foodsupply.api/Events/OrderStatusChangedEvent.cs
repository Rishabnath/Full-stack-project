namespace FoodSupply.Api.Events;

public sealed record OrderStatusChangedEvent(
    Guid EventId,
    int OrderId,
    string OrderNumber,
    Guid CustomerId,
    int RestaurantId,
    Guid? DeliveryPartnerId,
    string PreviousStatus,
    string CurrentStatus,
    Guid ChangedByUserId,
    string? Note,
    DateTime OccurredAtUtc);
