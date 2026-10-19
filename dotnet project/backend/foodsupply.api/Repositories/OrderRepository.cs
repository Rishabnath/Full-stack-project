using FoodSupply.Api.Data;
using FoodSupply.Api.Entities;
using FoodSupply.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

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

    public async Task AddAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Orders.AddAsync(order, cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
