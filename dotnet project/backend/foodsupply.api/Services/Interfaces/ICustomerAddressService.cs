using FoodSupply.Api.DTOs.Addresses;

namespace FoodSupply.Api.Services.Interfaces;

public interface ICustomerAddressService
{
    Task<IReadOnlyList<AddressResponse>> GetMineAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<AddressResponse> GetByIdAsync(
        Guid userId,
        int addressId,
        CancellationToken cancellationToken = default);

    Task<AddressResponse> CreateAsync(
        Guid userId,
        CreateAddressRequest request,
        CancellationToken cancellationToken = default);

    Task<AddressResponse> UpdateAsync(
        Guid userId,
        int addressId,
        UpdateAddressRequest request,
        CancellationToken cancellationToken = default);

    Task SetDefaultAsync(
        Guid userId,
        int addressId,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid userId,
        int addressId,
        CancellationToken cancellationToken = default);
}
