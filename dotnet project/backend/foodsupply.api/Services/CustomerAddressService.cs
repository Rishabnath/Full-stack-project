using FoodSupply.Api.DTOs.Addresses;
using FoodSupply.Api.Entities;
using FoodSupply.Api.Exceptions;
using FoodSupply.Api.Repositories.Interfaces;
using FoodSupply.Api.Services.Interfaces;

namespace FoodSupply.Api.Services;

public sealed class CustomerAddressService : ICustomerAddressService
{
    private readonly ICustomerAddressRepository _addressRepository;

    public CustomerAddressService(ICustomerAddressRepository addressRepository)
    {
        _addressRepository = addressRepository;
    }

    public async Task<IReadOnlyList<AddressResponse>> GetMineAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        ValidateUserId(userId);

        var addresses = await _addressRepository.GetAllByUserIdAsync(
            userId,
            cancellationToken);

        return addresses.Select(MapToResponse).ToList();
    }

    public async Task<AddressResponse> GetByIdAsync(
        Guid userId,
        int addressId,
        CancellationToken cancellationToken = default)
    {
        var address = await GetOwnedAddressAsync(userId, addressId, cancellationToken);
        return MapToResponse(address);
    }

    public async Task<AddressResponse> CreateAsync(
        Guid userId,
        CreateAddressRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateUserId(userId);

        if (request is null)
        {
            throw new BadRequestException("Address request body is required.");
        }

        var currentAddresses = await _addressRepository.GetAllByUserIdAsync(
            userId,
            cancellationToken);

        var address = new CustomerAddress
        {
            UserId = userId,
            Label = NormalizeRequiredText(request.Label, "Address label", 2, 60),
            Line1 = NormalizeRequiredText(request.Line1, "Address line 1", 5, 250),
            Line2 = NormalizeOptionalText(request.Line2, "Address line 2", 250),
            City = NormalizeRequiredText(request.City, "City", 2, 100),
            State = NormalizeRequiredText(request.State, "State", 2, 100),
            PostalCode = NormalizeRequiredText(request.PostalCode, "Postal code", 3, 20),
            Country = NormalizeRequiredText(request.Country, "Country", 2, 100),
            CreatedAtUtc = DateTime.UtcNow
        };

        // A new customer needs a usable checkout address, so their first saved
        // address becomes the default even if they omit IsDefault in the request.
        var makeDefault = request.IsDefault || currentAddresses.Count == 0;
        await _addressRepository.AddAsync(address, makeDefault, cancellationToken);

        return MapToResponse(address);
    }

    public async Task<AddressResponse> UpdateAsync(
        Guid userId,
        int addressId,
        UpdateAddressRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new BadRequestException("Address request body is required.");
        }

        var address = await GetOwnedAddressAsync(userId, addressId, cancellationToken);

        address.Label = NormalizeRequiredText(request.Label, "Address label", 2, 60);
        address.Line1 = NormalizeRequiredText(request.Line1, "Address line 1", 5, 250);
        address.Line2 = NormalizeOptionalText(request.Line2, "Address line 2", 250);
        address.City = NormalizeRequiredText(request.City, "City", 2, 100);
        address.State = NormalizeRequiredText(request.State, "State", 2, 100);
        address.PostalCode = NormalizeRequiredText(request.PostalCode, "Postal code", 3, 20);
        address.Country = NormalizeRequiredText(request.Country, "Country", 2, 100);

        await _addressRepository.SaveChangesAsync(cancellationToken);
        return MapToResponse(address);
    }

    public async Task SetDefaultAsync(
        Guid userId,
        int addressId,
        CancellationToken cancellationToken = default)
    {
        var address = await GetOwnedAddressAsync(userId, addressId, cancellationToken);

        if (!address.IsDefault)
        {
            await _addressRepository.SetDefaultAsync(
                userId,
                address,
                cancellationToken);
        }
    }

    public async Task DeleteAsync(
        Guid userId,
        int addressId,
        CancellationToken cancellationToken = default)
    {
        var address = await GetOwnedAddressAsync(userId, addressId, cancellationToken);
        await _addressRepository.DeleteAsync(userId, address, cancellationToken);
    }

    private async Task<CustomerAddress> GetOwnedAddressAsync(
        Guid userId,
        int addressId,
        CancellationToken cancellationToken)
    {
        ValidateUserId(userId);

        if (addressId <= 0)
        {
            throw new BadRequestException("Address ID must be greater than zero.");
        }

        var address = await _addressRepository.GetByIdAsync(
            userId,
            addressId,
            cancellationToken);

        return address ?? throw new NotFoundException(
            $"Address with ID {addressId} was not found.");
    }

    private static void ValidateUserId(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new UnauthorizedException("A valid authenticated user is required.");
        }
    }

    private static string NormalizeRequiredText(
        string? value,
        string fieldName,
        int minimumLength,
        int maximumLength)
    {
        var normalizedValue = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedValue))
        {
            throw new BadRequestException($"{fieldName} is required.");
        }

        if (normalizedValue.Length < minimumLength || normalizedValue.Length > maximumLength)
        {
            throw new BadRequestException(
                $"{fieldName} must be between {minimumLength} and {maximumLength} characters.");
        }

        return normalizedValue;
    }

    private static string? NormalizeOptionalText(
        string? value,
        string fieldName,
        int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalizedValue = value.Trim();
        if (normalizedValue.Length > maximumLength)
        {
            throw new BadRequestException($"{fieldName} cannot exceed {maximumLength} characters.");
        }

        return normalizedValue;
    }

    private static AddressResponse MapToResponse(CustomerAddress address)
    {
        return new AddressResponse
        {
            Id = address.Id,
            Label = address.Label,
            Line1 = address.Line1,
            Line2 = address.Line2,
            City = address.City,
            State = address.State,
            PostalCode = address.PostalCode,
            Country = address.Country,
            IsDefault = address.IsDefault,
            CreatedAtUtc = address.CreatedAtUtc
        };
    }
}
