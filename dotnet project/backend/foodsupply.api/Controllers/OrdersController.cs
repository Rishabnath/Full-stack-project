using System.Security.Claims;
using FoodSupply.Api.DTOs.Orders;
using FoodSupply.Api.Entities;
using FoodSupply.Api.Exceptions;
using FoodSupply.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodSupply.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet("mine")]
    [Authorize(Roles = nameof(UserRole.Customer))]
    [ProducesResponseType(typeof(OrderListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderListResponse>> GetMine(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var orders = await _orderService.GetMineAsync(
            GetCurrentUserId(),
            page,
            pageSize,
            cancellationToken);

        return Ok(orders);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Customer))]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> GetMineById(
        int id,
        CancellationToken cancellationToken)
    {
        var order = await _orderService.GetMineByIdAsync(
            GetCurrentUserId(),
            id,
            cancellationToken);

        return Ok(order);
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Customer))]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponse>> Create(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var order = await _orderService.CreateAsync(
            GetCurrentUserId(),
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetMineById),
            new { id = order.Id },
            order);
    }

    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = nameof(UserRole.Customer))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelMine(
        int id,
        CancellationToken cancellationToken)
    {
        await _orderService.CancelMineAsync(
            GetCurrentUserId(),
            id,
            cancellationToken);

        return NoContent();
    }

    [HttpGet("restaurant/{restaurantId:int}")]
    [Authorize(Roles = nameof(UserRole.RestaurantOwner) + "," + nameof(UserRole.Admin))]
    [ProducesResponseType(typeof(OrderListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderListResponse>> GetRestaurantOrders(
        int restaurantId,
        [FromQuery] OrderStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var orders = await _orderService.GetRestaurantOrdersAsync(
            GetCurrentUserId(),
            restaurantId,
            status,
            page,
            pageSize,
            cancellationToken);

        return Ok(orders);
    }

    [HttpGet("delivery/available")]
    [Authorize(Roles = nameof(UserRole.DeliveryPartner))]
    [ProducesResponseType(typeof(OrderListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderListResponse>> GetAvailableDeliveryOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var orders = await _orderService.GetAvailableDeliveryOrdersAsync(
            GetCurrentUserId(),
            page,
            pageSize,
            cancellationToken);

        return Ok(orders);
    }

    [HttpGet("delivery/mine")]
    [Authorize(Roles = nameof(UserRole.DeliveryPartner))]
    [ProducesResponseType(typeof(OrderListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderListResponse>> GetMyDeliveryOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var orders = await _orderService.GetMyDeliveryOrdersAsync(
            GetCurrentUserId(),
            page,
            pageSize,
            cancellationToken);

        return Ok(orders);
    }

    [HttpGet("{id:int}/operations")]
    [Authorize(Roles = nameof(UserRole.RestaurantOwner) + "," + nameof(UserRole.Admin) + "," + nameof(UserRole.DeliveryPartner))]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> GetOperationalDetails(
        int id,
        CancellationToken cancellationToken)
    {
        var order = await _orderService.GetOperationalByIdAsync(
            GetCurrentUserId(),
            id,
            cancellationToken);

        return Ok(order);
    }

    [HttpPost("{id:int}/claim")]
    [Authorize(Roles = nameof(UserRole.DeliveryPartner))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ClaimDelivery(
        int id,
        CancellationToken cancellationToken)
    {
        await _orderService.ClaimDeliveryAsync(
            GetCurrentUserId(),
            id,
            cancellationToken);

        return NoContent();
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = nameof(UserRole.RestaurantOwner) + "," + nameof(UserRole.Admin) + "," + nameof(UserRole.DeliveryPartner))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeStatus(
        int id,
        [FromBody] UpdateOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        await _orderService.ChangeStatusAsync(
            GetCurrentUserId(),
            id,
            request,
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
