using FoodSupply.Api.Data;
using FoodSupply.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FoodSupply.Api.Security;

/// <summary>
/// Creates the one bootstrap Administrator when credentials are supplied in
/// user secrets or another deployment secret store. It never exposes a public
/// Administrator registration endpoint.
/// </summary>
public sealed class InitialAdministratorSeeder
{
    private readonly FoodSupplyDbContext _dbContext;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<InitialAdministratorSeeder> _logger;

    public InitialAdministratorSeeder(
        FoodSupplyDbContext dbContext,
        IPasswordHasher<ApplicationUser> passwordHasher,
        IConfiguration configuration,
        ILogger<InitialAdministratorSeeder> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedIfConfiguredAsync(
        CancellationToken cancellationToken = default)
    {
        var email = _configuration["BootstrapAdmin:Email"];
        var password = _configuration["BootstrapAdmin:Password"];

        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(password))
        {
            _logger.LogInformation(
                "Bootstrap Administrator credentials are not configured; skipping initial administrator creation.");
            return;
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Configure both BootstrapAdmin:Email and BootstrapAdmin:Password in user secrets or a deployment secret store.");
        }

        if (password.Length < 12)
        {
            throw new InvalidOperationException(
                "The bootstrap Administrator password must contain at least 12 characters.");
        }

        if (await _dbContext.Users.AnyAsync(
                user => user.Role == UserRole.Admin,
                cancellationToken))
        {
            _logger.LogInformation(
                "An Administrator already exists; bootstrap creation was skipped.");
            return;
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (await _dbContext.Users.AnyAsync(
                user => user.Email == normalizedEmail,
                cancellationToken))
        {
            throw new InvalidOperationException(
                "The bootstrap email already belongs to a non-Administrator account. Configure a different email.");
        }

        var administrator = new ApplicationUser
        {
            FirstName = "FoodSupply",
            LastName = "Administrator",
            Email = normalizedEmail,
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        administrator.PasswordHash = _passwordHasher.HashPassword(
            administrator,
            password);

        await _dbContext.Users.AddAsync(administrator, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created the initial FoodSupply Administrator account {AdministratorEmail}.",
            administrator.Email);
    }
}
