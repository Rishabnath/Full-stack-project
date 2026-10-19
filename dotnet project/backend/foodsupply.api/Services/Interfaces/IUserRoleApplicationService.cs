using FoodSupply.Api.DTOs.RoleApplications;

namespace FoodSupply.Api.Services.Interfaces;

public interface IUserRoleApplicationService
{
    Task<RoleApplicationResponse> ApplyAsync(
        Guid applicantUserId,
        CreateRoleApplicationRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoleApplicationResponse>> GetMineAsync(
        Guid applicantUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoleApplicationResponse>> GetPendingAsync(
        Guid administratorUserId,
        CancellationToken cancellationToken = default);

    Task<RoleApplicationResponse> ApproveAsync(
        Guid applicationId,
        Guid reviewerUserId,
        ReviewRoleApplicationRequest request,
        CancellationToken cancellationToken = default);

    Task<RoleApplicationResponse> RejectAsync(
        Guid applicationId,
        Guid reviewerUserId,
        ReviewRoleApplicationRequest request,
        CancellationToken cancellationToken = default);
}
