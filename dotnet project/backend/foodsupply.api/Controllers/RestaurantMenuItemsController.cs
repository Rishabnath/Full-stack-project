using System.Security.Claims;
using FoodSupply.Api.DTOs.MenuItems;
using FoodSupply.Api.Entities;
using FoodSupply.Api.Exceptions;
using FoodSupply.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodSupply.Api.Controllers;

[ApiController]
[Route("api/restaurants/{restaurantId:int}/menu-items")]
public sealed class RestaurantMenuItemsController : ControllerBase
{
    private const string RestaurantManagerRoles =
        nameof(UserRole.RestaurantOwner) + "," + nameof(UserRole.Admin);

    private readonly IMenuItemService _menuItemService;

    public RestaurantMenuItemsController(IMenuItemService menuItemService)
    {
        _menuItemService = menuItemService;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<MenuItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<MenuItemResponse>>> GetAvailable(
        int restaurantId,
        CancellationToken cancellationToken)
    {
        var response = await _menuItemService.GetAvailableForRestaurantAsync(
            restaurantId,
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{menuItemId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(MenuItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MenuItemResponse>> GetById(
        int restaurantId,
        int menuItemId,
        CancellationToken cancellationToken)
    {
        var response = await _menuItemService.GetAvailableByIdAsync(
            restaurantId,
            menuItemId,
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("manage")]
    [Authorize(Roles = RestaurantManagerRoles)]
    [ProducesResponseType(typeof(IReadOnlyList<MenuItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<MenuItemResponse>>> GetForManagement(
        int restaurantId,
        CancellationToken cancellationToken)
    {
        var callerUserId = GetCurrentUserId();
        var response = await _menuItemService.GetManageableForRestaurantAsync(
            restaurantId,
            callerUserId,
            cancellationToken);

        return Ok(response);
    }

    [HttpPost]
    [Authorize(Roles = RestaurantManagerRoles)]
    [ProducesResponseType(typeof(MenuItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MenuItemResponse>> Create(
        int restaurantId,
        [FromBody] CreateMenuItemRequest request,
        CancellationToken cancellationToken)
    {
        var callerUserId = GetCurrentUserId();
        var response = await _menuItemService.CreateAsync(
            restaurantId,
            callerUserId,
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { restaurantId, menuItemId = response.Id },
            response);
    }

    [HttpPut("{menuItemId:int}")]
    [Authorize(Roles = RestaurantManagerRoles)]
    [ProducesResponseType(typeof(MenuItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MenuItemResponse>> Update(
        int restaurantId,
        int menuItemId,
        [FromBody] UpdateMenuItemRequest request,
        CancellationToken cancellationToken)
    {
        var callerUserId = GetCurrentUserId();
        var response = await _menuItemService.UpdateAsync(
            restaurantId,
            menuItemId,
            callerUserId,
            request,
            cancellationToken);

        return Ok(response);
    }

    [HttpDelete("{menuItemId:int}")]
    [Authorize(Roles = RestaurantManagerRoles)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(
        int restaurantId,
        int menuItemId,
        CancellationToken cancellationToken)
    {
        var callerUserId = GetCurrentUserId();
        await _menuItemService.DeactivateAsync(
            restaurantId,
            menuItemId,
            callerUserId,
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
