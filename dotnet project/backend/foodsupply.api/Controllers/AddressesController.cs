using System.Security.Claims;
using FoodSupply.Api.DTOs.Addresses;
using FoodSupply.Api.Entities;
using FoodSupply.Api.Exceptions;
using FoodSupply.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodSupply.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = nameof(UserRole.Customer))]
public sealed class AddressesController : ControllerBase
{
    private readonly ICustomerAddressService _addressService;

    public AddressesController(ICustomerAddressService addressService)
    {
        _addressService = addressService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AddressResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<AddressResponse>>> GetMine(
        CancellationToken cancellationToken)
    {
        var addresses = await _addressService.GetMineAsync(
            GetCurrentUserId(),
            cancellationToken);

        return Ok(addresses);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AddressResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AddressResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var address = await _addressService.GetByIdAsync(
            GetCurrentUserId(),
            id,
            cancellationToken);

        return Ok(address);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AddressResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AddressResponse>> Create(
        [FromBody] CreateAddressRequest request,
        CancellationToken cancellationToken)
    {
        var address = await _addressService.CreateAsync(
            GetCurrentUserId(),
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = address.Id },
            address);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(AddressResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AddressResponse>> Update(
        int id,
        [FromBody] UpdateAddressRequest request,
        CancellationToken cancellationToken)
    {
        var address = await _addressService.UpdateAsync(
            GetCurrentUserId(),
            id,
            request,
            cancellationToken);

        return Ok(address);
    }

    [HttpPut("{id:int}/default")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetDefault(
        int id,
        CancellationToken cancellationToken)
    {
        await _addressService.SetDefaultAsync(
            GetCurrentUserId(),
            id,
            cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        await _addressService.DeleteAsync(
            GetCurrentUserId(),
            id,
            cancellationToken);

        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var subject = User.FindFirstValue("sub");

        if (!Guid.TryParse(subject, out var userId) || userId == Guid.Empty)
        {
            throw new UnauthorizedException(
                "The access token does not contain a valid user ID.");
        }

        return userId;
    }
}
