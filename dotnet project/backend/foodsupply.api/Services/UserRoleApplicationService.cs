using FoodSupply.Api.DTOs.RoleApplications;
using FoodSupply.Api.Caching;
using FoodSupply.Api.Entities;
using FoodSupply.Api.Exceptions;
using FoodSupply.Api.Repositories.Interfaces;
using FoodSupply.Api.Services.Interfaces;

namespace FoodSupply.Api.Services;

public sealed class UserRoleApplicationService : IUserRoleApplicationService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserRoleApplicationRepository _applicationRepository;
    private readonly IAppCache _cache;

    public UserRoleApplicationService(
        IUserRepository userRepository,
        IUserRoleApplicationRepository applicationRepository,
        IAppCache cache)
    {
        _userRepository = userRepository;
        _applicationRepository = applicationRepository;
        _cache = cache;
    }

    public async Task<RoleApplicationResponse> ApplyAsync(
        Guid applicantUserId,
        CreateRoleApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateUserId(applicantUserId);

        if (request is null)
        {
            throw new BadRequestException("Role application request body is required.");
        }

        if (request.RequestedRole is not UserRole.RestaurantOwner and
            not UserRole.DeliveryPartner)
        {
            throw new BadRequestException(
                "You may apply only for the RestaurantOwner or DeliveryPartner role.");
        }

        var applicationDetails = request.ApplicationDetails?.Trim();

        if (string.IsNullOrWhiteSpace(applicationDetails) ||
            applicationDetails.Length < 20 ||
            applicationDetails.Length > 1500)
        {
            throw new BadRequestException(
                "Application details must be between 20 and 1500 characters.");
        }

        var businessName = NormalizeBusinessName(request.BusinessName);

        if (request.RequestedRole == UserRole.RestaurantOwner && businessName is null)
        {
            throw new BadRequestException(
                "Business name is required for a RestaurantOwner application.");
        }

        var applicant = await _userRepository.GetByIdAsync(
            applicantUserId,
            cancellationToken);

        if (applicant is null || !applicant.IsActive)
        {
            throw new UnauthorizedException("Your account is unavailable.");
        }

        if (applicant.Role != UserRole.Customer)
        {
            throw new ForbiddenException(
                "Only customer accounts can apply for a business or delivery role.");
        }

        if (await _applicationRepository.HasPendingForApplicantAsync(
                applicantUserId,
                cancellationToken))
        {
            throw new ConflictException(
                "You already have a pending role application. Wait for an administrator to review it.");
        }

        var application = new UserRoleApplication
        {
            ApplicantUserId = applicantUserId,
            RequestedRole = request.RequestedRole,
            Status = UserRoleApplicationStatus.Pending,
            BusinessName = businessName,
            ApplicationDetails = applicationDetails,
            SubmittedAtUtc = DateTime.UtcNow
        };

        await _applicationRepository.AddAsync(application, cancellationToken);
        await _applicationRepository.SaveChangesAsync(cancellationToken);
        InvalidateApplications(applicantUserId);

        application.Applicant = applicant;
        return MapToResponse(application);
    }

    public async Task<IReadOnlyList<RoleApplicationResponse>> GetMineAsync(
        Guid applicantUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateUserId(applicantUserId);
        await EnsureActiveUserAsync(applicantUserId, cancellationToken);

        return await _cache.GetOrCreateAsync(
            $"role-applications:user:{applicantUserId:D}",
            CacheDurations.UserData,
            async token =>
            {
                var applications = await _applicationRepository.GetByApplicantIdAsync(
                    applicantUserId,
                    token);

                return applications.Select(MapToResponse).ToList();
            },
            cancellationToken,
            CacheTags.RoleApplications);
    }

    public async Task<IReadOnlyList<RoleApplicationResponse>> GetPendingAsync(
        Guid administratorUserId,
        CancellationToken cancellationToken = default)
    {
        await GetActiveAdministratorAsync(administratorUserId, cancellationToken);

        return await _cache.GetOrCreateAsync(
            "role-applications:pending",
            CacheDurations.UserData,
            async token =>
            {
                var applications = await _applicationRepository.GetPendingAsync(token);
                return applications.Select(MapToResponse).ToList();
            },
            cancellationToken,
            CacheTags.RoleApplications);
    }

    public async Task<RoleApplicationResponse> ApproveAsync(
        Guid applicationId,
        Guid reviewerUserId,
        ReviewRoleApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        var reviewer = await GetActiveAdministratorAsync(reviewerUserId, cancellationToken);
        var application = await GetPendingApplicationAsync(applicationId, cancellationToken);

        if (!application.Applicant.IsActive ||
            application.Applicant.Role != UserRole.Customer)
        {
            throw new ConflictException(
                "The applicant account is no longer eligible for this role change.");
        }

        application.Status = UserRoleApplicationStatus.Approved;
        application.ReviewNote = NormalizeReviewNote(request?.ReviewNote);
        application.ReviewedByUserId = reviewer.Id;
        application.ReviewedAtUtc = DateTime.UtcNow;
        application.Applicant.Role = application.RequestedRole;

        await _applicationRepository.SaveChangesAsync(cancellationToken);
        InvalidateApplications(application.ApplicantUserId);

        return MapToResponse(application);
    }

    public async Task<RoleApplicationResponse> RejectAsync(
        Guid applicationId,
        Guid reviewerUserId,
        ReviewRoleApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        var reviewer = await GetActiveAdministratorAsync(reviewerUserId, cancellationToken);
        var application = await GetPendingApplicationAsync(applicationId, cancellationToken);
        var reviewNote = NormalizeReviewNote(request?.ReviewNote);

        if (reviewNote is null)
        {
            throw new BadRequestException(
                "A review note explaining the rejection is required.");
        }

        application.Status = UserRoleApplicationStatus.Rejected;
        application.ReviewNote = reviewNote;
        application.ReviewedByUserId = reviewer.Id;
        application.ReviewedAtUtc = DateTime.UtcNow;

        await _applicationRepository.SaveChangesAsync(cancellationToken);
        InvalidateApplications(application.ApplicantUserId);

        return MapToResponse(application);
    }

    private async Task<ApplicationUser> GetActiveAdministratorAsync(
        Guid administratorUserId,
        CancellationToken cancellationToken)
    {
        var administrator = await _userRepository.GetByIdAsync(
            administratorUserId,
            cancellationToken);

        if (administrator is null ||
            !administrator.IsActive ||
            administrator.Role != UserRole.Admin)
        {
            throw new ForbiddenException("An active administrator account is required.");
        }

        return administrator;
    }

    private async Task EnsureActiveUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedException("Your account is unavailable.");
        }
    }

    private async Task<UserRoleApplication> GetPendingApplicationAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        if (applicationId == Guid.Empty)
        {
            throw new BadRequestException("Application ID is required.");
        }

        var application = await _applicationRepository.GetByIdAsync(
            applicationId,
            cancellationToken);

        if (application is null)
        {
            throw new NotFoundException(
                $"Role application with ID '{applicationId}' was not found.");
        }

        if (application.Status != UserRoleApplicationStatus.Pending)
        {
            throw new ConflictException("This application has already been reviewed.");
        }

        return application;
    }

    private static string? NormalizeBusinessName(string? businessName)
    {
        return string.IsNullOrWhiteSpace(businessName)
            ? null
            : businessName.Trim();
    }

    private static string? NormalizeReviewNote(string? reviewNote)
    {
        if (string.IsNullOrWhiteSpace(reviewNote))
        {
            return null;
        }

        var normalizedNote = reviewNote.Trim();
        if (normalizedNote.Length > 500)
        {
            throw new BadRequestException("Review note cannot exceed 500 characters.");
        }

        return normalizedNote;
    }

    private static void ValidateUserId(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new UnauthorizedException("A valid authenticated user is required.");
        }
    }

    private void InvalidateApplications(Guid applicantUserId)
    {
        _cache.EvictTag(CacheTags.RoleApplications);
    }

    private static RoleApplicationResponse MapToResponse(
        UserRoleApplication application)
    {
        return new RoleApplicationResponse
        {
            Id = application.Id,
            ApplicantUserId = application.ApplicantUserId,
            ApplicantName = $"{application.Applicant.FirstName} {application.Applicant.LastName}".Trim(),
            ApplicantEmail = application.Applicant.Email,
            RequestedRole = application.RequestedRole,
            Status = application.Status,
            BusinessName = application.BusinessName,
            ApplicationDetails = application.ApplicationDetails,
            ReviewNote = application.ReviewNote,
            SubmittedAtUtc = application.SubmittedAtUtc,
            ReviewedAtUtc = application.ReviewedAtUtc
        };
    }
}
