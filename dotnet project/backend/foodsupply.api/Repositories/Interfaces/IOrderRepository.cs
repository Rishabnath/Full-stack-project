using FoodSupply.Api.Entities;

namespace FoodSupply.Api.Repositories.Interfaces;

public interface IOrderRepository
{
    Task<(IReadOnlyList<Order> Orders, int TotalCount)> GetPageByCustomerIdAsync(
        Guid customerId,
        int skip,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Order?> GetByIdForCustomerAsync(
        Guid customerId,
        int orderId,
        CancellationToken cancellationToken = default);

    Task<Order?> GetByIdForWorkflowAsync(
        int orderId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Order> Orders, int TotalCount)> GetPageByRestaurantIdAsync(
        int restaurantId,
        OrderStatus? status,
        int skip,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Order> Orders, int TotalCount)> GetAvailableForDeliveryPageAsync(
        int skip,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Order> Orders, int TotalCount)> GetDeliveryPartnerPageAsync(
        Guid deliveryPartnerId,
        int skip,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<bool> TryClaimDeliveryAsync(
        int orderId,
        Guid deliveryPartnerId,
        CancellationToken cancellationToken = default);

    Task<bool> TryTransitionStatusAsync(
        int orderId,
        OrderStatus expectedStatus,
        OrderStatus newStatus,
        Guid actorUserId,
        bool requireAssignedDeliveryPartner,
        string? note,
        CancellationToken cancellationToken = default);

    Task CreateWithOutboxAsync(
        Order order,
        CancellationToken cancellationToken = default);
}
