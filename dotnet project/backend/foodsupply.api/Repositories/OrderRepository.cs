using FoodSupply.Api.Data;
using FoodSupply.Api.Entities;
using FoodSupply.Api.Events;
using FoodSupply.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FoodSupply.Api.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private readonly FoodSupplyDbContext _dbContext;

    public OrderRepository(FoodSupplyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(IReadOnlyList<Order> Orders, int TotalCount)> GetPageByCustomerIdAsync(
        Guid customerId,
        int skip,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Orders
            .AsNoTracking()
            .Where(order => order.CustomerId == customerId);

        var totalCount = await query.CountAsync(cancellationToken);
        var orders = await query
            .Include(order => order.Restaurant)
            .OrderByDescending(order => order.PlacedAtUtc)
            .ThenByDescending(order => order.Id)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (orders, totalCount);
    }

    public Task<Order?> GetByIdForCustomerAsync(
        Guid customerId,
        int orderId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Orders
            .AsNoTracking()
            .AsSplitQuery()
            .Include(order => order.Restaurant)
            .Include(order => order.Items)
            .Include(order => order.Payments)
            .Include(order => order.StatusHistory)
            .FirstOrDefaultAsync(
                order => order.CustomerId == customerId && order.Id == orderId,
                cancellationToken);
    }

    public Task<Order?> GetByIdForWorkflowAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Orders
            .AsNoTracking()
            .AsSplitQuery()
            .Include(order => order.Restaurant)
            .Include(order => order.Items)
            .Include(order => order.Payments)
            .Include(order => order.StatusHistory)
            .FirstOrDefaultAsync(order => order.Id == orderId, cancellationToken);
    }

    public async Task<(IReadOnlyList<Order> Orders, int TotalCount)> GetPageByRestaurantIdAsync(
        int restaurantId,
        OrderStatus? status,
        int skip,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Order> query = _dbContext.Orders
            .AsNoTracking()
            .Where(order => order.RestaurantId == restaurantId);

        if (status.HasValue)
        {
            query = query.Where(order => order.Status == status.Value);
        }

        return await GetSummaryPageAsync(query, skip, pageSize, cancellationToken);
    }

    public async Task<(IReadOnlyList<Order> Orders, int TotalCount)> GetAvailableForDeliveryPageAsync(
        int skip,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Orders
            .AsNoTracking()
            .Where(order =>
                order.Status == OrderStatus.Packed &&
                order.DeliveryPartnerId == null);

        return await GetSummaryPageAsync(query, skip, pageSize, cancellationToken);
    }

    public async Task<(IReadOnlyList<Order> Orders, int TotalCount)> GetDeliveryPartnerPageAsync(
        Guid deliveryPartnerId,
        int skip,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Orders
            .AsNoTracking()
            .Where(order => order.DeliveryPartnerId == deliveryPartnerId);

        return await GetSummaryPageAsync(query, skip, pageSize, cancellationToken);
    }

    public async Task<bool> TryClaimDeliveryAsync(
        int orderId,
        Guid deliveryPartnerId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        var changedAtUtc = DateTime.UtcNow;
        var affectedRows = await _dbContext.Orders
            .Where(order =>
                order.Id == orderId &&
                order.Status == OrderStatus.Packed &&
                order.DeliveryPartnerId == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(order => order.DeliveryPartnerId, deliveryPartnerId)
                    .SetProperty(order => order.UpdatedAtUtc, changedAtUtc),
                cancellationToken);

        if (affectedRows == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var orderDetails = await _dbContext.Orders
            .AsNoTracking()
            .Where(order => order.Id == orderId)
            .Select(order => new
            {
                order.OrderNumber,
                order.CustomerId,
                order.RestaurantId
            })
            .SingleAsync(cancellationToken);

        var deliveryClaimedEvent = new OrderDeliveryClaimedEvent(
            EventId: Guid.NewGuid(),
            OrderId: orderId,
            OrderNumber: orderDetails.OrderNumber,
            CustomerId: orderDetails.CustomerId,
            RestaurantId: orderDetails.RestaurantId,
            DeliveryPartnerId: deliveryPartnerId,
            CurrentStatus: OrderStatus.Packed.ToString(),
            OccurredAtUtc: changedAtUtc);

        await _dbContext.OrderStatusHistory.AddAsync(
            new OrderStatusHistory
            {
                OrderId = orderId,
                Status = OrderStatus.Packed,
                ChangedAtUtc = changedAtUtc,
                Note = "Claimed by a delivery partner.",
                ChangedByUserId = deliveryPartnerId
            },
            cancellationToken);

        await _dbContext.OrderOutboxMessages.AddAsync(
            new OrderOutboxMessage
            {
                Id = deliveryClaimedEvent.EventId,
                EventType = nameof(OrderDeliveryClaimedEvent),
                RoutingKey = "orders.delivery.claimed",
                Payload = JsonSerializer.Serialize(deliveryClaimedEvent),
                OccurredAtUtc = changedAtUtc,
                NextAttemptAtUtc = changedAtUtc
            },
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TryTransitionStatusAsync(
        int orderId,
        OrderStatus expectedStatus,
        OrderStatus newStatus,
        Guid actorUserId,
        bool requireAssignedDeliveryPartner,
        string? note,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        IQueryable<Order> query = _dbContext.Orders
            .Where(order => order.Id == orderId && order.Status == expectedStatus);

        if (requireAssignedDeliveryPartner)
        {
            query = query.Where(order => order.DeliveryPartnerId == actorUserId);
        }

        var changedAtUtc = DateTime.UtcNow;
        var affectedRows = await query.ExecuteUpdateAsync(
            setters => setters
                .SetProperty(order => order.Status, newStatus)
                .SetProperty(order => order.UpdatedAtUtc, changedAtUtc),
            cancellationToken);

        if (affectedRows == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var orderDetails = await _dbContext.Orders
            .AsNoTracking()
            .Where(order => order.Id == orderId)
            .Select(order => new
            {
                order.OrderNumber,
                order.CustomerId,
                order.RestaurantId,
                order.DeliveryPartnerId
            })
            .SingleAsync(cancellationToken);

        var statusChangedEvent = new OrderStatusChangedEvent(
            EventId: Guid.NewGuid(),
            OrderId: orderId,
            OrderNumber: orderDetails.OrderNumber,
            CustomerId: orderDetails.CustomerId,
            RestaurantId: orderDetails.RestaurantId,
            DeliveryPartnerId: orderDetails.DeliveryPartnerId,
            PreviousStatus: expectedStatus.ToString(),
            CurrentStatus: newStatus.ToString(),
            ChangedByUserId: actorUserId,
            Note: note,
            OccurredAtUtc: changedAtUtc);

        await _dbContext.OrderStatusHistory.AddAsync(
            new OrderStatusHistory
            {
                OrderId = orderId,
                Status = newStatus,
                ChangedAtUtc = changedAtUtc,
                Note = note,
                ChangedByUserId = actorUserId
            },
            cancellationToken);

        await _dbContext.OrderOutboxMessages.AddAsync(
            new OrderOutboxMessage
            {
                Id = statusChangedEvent.EventId,
                EventType = nameof(OrderStatusChangedEvent),
                RoutingKey = "orders.status.changed",
                Payload = JsonSerializer.Serialize(statusChangedEvent),
                OccurredAtUtc = changedAtUtc,
                NextAttemptAtUtc = changedAtUtc
            },
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task<(IReadOnlyList<Order> Orders, int TotalCount)> GetSummaryPageAsync(
        IQueryable<Order> query,
        int skip,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var orders = await query
            .Include(order => order.Restaurant)
            .OrderByDescending(order => order.PlacedAtUtc)
            .ThenByDescending(order => order.Id)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (orders, totalCount);
    }

    public async Task CreateWithOutboxAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        await _dbContext.Orders.AddAsync(order, cancellationToken);

        // Save once inside the open transaction to obtain PostgreSQL's generated order ID.
        await _dbContext.SaveChangesAsync(cancellationToken);

        var orderPlacedEvent = new OrderPlacedEvent(
            EventId: Guid.NewGuid(),
            OrderId: order.Id,
            OrderNumber: order.OrderNumber,
            CustomerId: order.CustomerId,
            RestaurantId: order.RestaurantId,
            TotalAmount: order.TotalAmount,
            CurrencyCode: order.CurrencyCode,
            OccurredAtUtc: order.PlacedAtUtc);

        await _dbContext.OrderOutboxMessages.AddAsync(
            new OrderOutboxMessage
            {
                Id = orderPlacedEvent.EventId,
                EventType = nameof(OrderPlacedEvent),
                RoutingKey = "orders.placed",
                Payload = JsonSerializer.Serialize(orderPlacedEvent),
                OccurredAtUtc = order.PlacedAtUtc,
                NextAttemptAtUtc = order.PlacedAtUtc
            },
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
