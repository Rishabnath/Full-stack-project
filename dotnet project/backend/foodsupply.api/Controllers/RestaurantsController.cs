using System.Security.Claims;
using FoodSupply.Api.DTOs.Restaurants;
using FoodSupply.Api.Entities;
using FoodSupply.Api.Exceptions;
using FoodSupply.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodSupply.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class RestaurantsController : ControllerBase
{
    private readonly IRestaurantService _restaurantService;

    public RestaurantsController(IRestaurantService restaurantService)
    {
        _restaurantService = restaurantService;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RestaurantListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RestaurantListResponse>> GetActivePage(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? city = null,
        CancellationToken cancellationToken = default)
    {
        var response = await _restaurantService.GetActivePageAsync(
            page,
            pageSize,
            city,
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RestaurantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RestaurantResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var response = await _restaurantService.GetActiveByIdAsync(
            id,
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("mine")]
    [Authorize(Roles = nameof(UserRole.RestaurantOwner))]
    [ProducesResponseType(typeof(IReadOnlyList<RestaurantResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<RestaurantResponse>>> GetMine(
        CancellationToken cancellationToken)
    {
        var ownerId = GetCurrentUserId();
        var response = await _restaurantService.GetMineAsync(
            ownerId,
            cancellationToken);

        return Ok(response);
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.RestaurantOwner))]
    [ProducesResponseType(typeof(RestaurantResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RestaurantResponse>> Create(
        [FromBody] CreateRestaurantRequest request,
        CancellationToken cancellationToken)
    {
        var ownerId = GetCurrentUserId();
        var response = await _restaurantService.CreateAsync(
            ownerId,
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = response.Id },
            response);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = nameof(UserRole.RestaurantOwner) + "," + nameof(UserRole.Admin))]
    [ProducesResponseType(typeof(RestaurantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RestaurantResponse>> Update(
        int id,
        [FromBody] UpdateRestaurantRequest request,
        CancellationToken cancellationToken)
    {
        var callerUserId = GetCurrentUserId();
        var response = await _restaurantService.UpdateAsync(
            id,
            callerUserId,
            request,
            cancellationToken);

        return Ok(response);
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
