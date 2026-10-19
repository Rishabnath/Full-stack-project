using FoodSupply.Api.Entities;

namespace FoodSupply.Api.Repositories.Interfaces;

public interface IUserRoleApplicationRepository
{
    Task<bool> HasPendingForApplicantAsync(
        Guid applicantUserId,
        CancellationToken cancellationToken = default);

    Task<UserRoleApplication?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserRoleApplication>> GetByApplicantIdAsync(
        Guid applicantUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserRoleApplication>> GetPendingAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        UserRoleApplication application,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
