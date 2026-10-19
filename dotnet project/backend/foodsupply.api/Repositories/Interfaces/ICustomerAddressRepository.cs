using FoodSupply.Api.Entities;

namespace FoodSupply.Api.Repositories.Interfaces;

public interface ICustomerAddressRepository
{
    Task<IReadOnlyList<CustomerAddress>> GetAllByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<CustomerAddress?> GetByIdAsync(
        Guid userId,
        int addressId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        CustomerAddress address,
        bool makeDefault,
        CancellationToken cancellationToken = default);

    Task SetDefaultAsync(
        Guid userId,
        CustomerAddress address,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid userId,
        CustomerAddress address,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
