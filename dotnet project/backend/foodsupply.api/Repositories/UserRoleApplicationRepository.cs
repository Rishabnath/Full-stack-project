using FoodSupply.Api.Data;
using FoodSupply.Api.Entities;
using FoodSupply.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FoodSupply.Api.Repositories;

public sealed class UserRoleApplicationRepository : IUserRoleApplicationRepository
{
    private readonly FoodSupplyDbContext _dbContext;

    public UserRoleApplicationRepository(FoodSupplyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> HasPendingForApplicantAsync(
        Guid applicantUserId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.UserRoleApplications
            .AsNoTracking()
            .AnyAsync(
                application =>
                    application.ApplicantUserId == applicantUserId &&
                    application.Status == UserRoleApplicationStatus.Pending,
                cancellationToken);
    }

    public Task<UserRoleApplication?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.UserRoleApplications
            .Include(application => application.Applicant)
            .FirstOrDefaultAsync(application => application.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<UserRoleApplication>> GetByApplicantIdAsync(
        Guid applicantUserId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserRoleApplications
            .AsNoTracking()
            .Include(application => application.Applicant)
            .Where(application => application.ApplicantUserId == applicantUserId)
            .OrderByDescending(application => application.SubmittedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserRoleApplication>> GetPendingAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserRoleApplications
            .AsNoTracking()
            .Include(application => application.Applicant)
            .Where(application => application.Status == UserRoleApplicationStatus.Pending)
            .OrderBy(application => application.SubmittedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        UserRoleApplication application,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.UserRoleApplications.AddAsync(application, cancellationToken);
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
