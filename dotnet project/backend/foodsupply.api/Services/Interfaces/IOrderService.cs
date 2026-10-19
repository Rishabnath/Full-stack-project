using FoodSupply.Api.DTOs.Orders;
using FoodSupply.Api.Entities;

namespace FoodSupply.Api.Services.Interfaces;

public interface IOrderService
{
    Task<OrderListResponse> GetMineAsync(
        Guid customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<OrderResponse> GetMineByIdAsync(
        Guid customerId,
        int orderId,
        CancellationToken cancellationToken = default);

    Task<OrderResponse> CreateAsync(
        Guid customerId,
        CreateOrderRequest request,
        CancellationToken cancellationToken = default);

    Task<OrderListResponse> GetRestaurantOrdersAsync(
        Guid callerUserId,
        int restaurantId,
        OrderStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<OrderListResponse> GetAvailableDeliveryOrdersAsync(
        Guid deliveryPartnerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<OrderListResponse> GetMyDeliveryOrdersAsync(
        Guid deliveryPartnerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<OrderResponse> GetOperationalByIdAsync(
        Guid callerUserId,
        int orderId,
        CancellationToken cancellationToken = default);

    Task ClaimDeliveryAsync(
        Guid deliveryPartnerId,
        int orderId,
        CancellationToken cancellationToken = default);

    Task ChangeStatusAsync(
        Guid callerUserId,
        int orderId,
        UpdateOrderStatusRequest request,
        CancellationToken cancellationToken = default);
}
