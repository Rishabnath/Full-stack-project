using System.Security.Claims;
using FoodSupply.Api.DTOs.RoleApplications;
using FoodSupply.Api.Entities;
using FoodSupply.Api.Exceptions;
using FoodSupply.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodSupply.Api.Controllers;

[ApiController]
[Route("api/role-applications")]
public sealed class UserRoleApplicationsController : ControllerBase
{
    private readonly IUserRoleApplicationService _applicationService;

    public UserRoleApplicationsController(
        IUserRoleApplicationService applicationService)
    {
        _applicationService = applicationService;
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Customer))]
    [ProducesResponseType(typeof(RoleApplicationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RoleApplicationResponse>> Apply(
        [FromBody] CreateRoleApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _applicationService.ApplyAsync(
            GetCurrentUserId(),
            request,
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("mine")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<RoleApplicationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<RoleApplicationResponse>>> GetMine(
        CancellationToken cancellationToken)
    {
        var applications = await _applicationService.GetMineAsync(
            GetCurrentUserId(),
            cancellationToken);

        return Ok(applications);
    }

    [HttpGet("pending")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType(typeof(IReadOnlyList<RoleApplicationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<RoleApplicationResponse>>> GetPending(
        CancellationToken cancellationToken)
    {
        var applications = await _applicationService.GetPendingAsync(cancellationToken);
        return Ok(applications);
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType(typeof(RoleApplicationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RoleApplicationResponse>> Approve(
        Guid id,
        [FromBody] ReviewRoleApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var application = await _applicationService.ApproveAsync(
            id,
            GetCurrentUserId(),
            request,
            cancellationToken);

        return Ok(application);
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType(typeof(RoleApplicationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RoleApplicationResponse>> Reject(
        Guid id,
        [FromBody] ReviewRoleApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var application = await _applicationService.RejectAsync(
            id,
            GetCurrentUserId(),
            request,
            cancellationToken);

        return Ok(application);
    }

    private Guid GetCurrentUserId()
    {
        var subject = User.FindFirstValue("sub");

        if (!Guid.TryParse(subject, out var userId))
        {
            throw new UnauthorizedException("The access token does not contain a valid user ID.");
        }

        return userId;
    }
}
