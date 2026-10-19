namespace FoodSupply.Api.Events;

public sealed record OrderDeliveryClaimedEvent(
    Guid EventId,
    int OrderId,
    string OrderNumber,
    Guid CustomerId,
    int RestaurantId,
    Guid DeliveryPartnerId,
    string CurrentStatus,
    DateTime OccurredAtUtc);
