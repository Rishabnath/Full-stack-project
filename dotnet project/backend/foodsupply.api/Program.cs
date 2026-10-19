using System.Text;
using System.Text.Json.Serialization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using FoodSupply.Api.Data;
using FoodSupply.Api.Caching;
using FoodSupply.Api.Entities;
using FoodSupply.Api.Exceptions;
using FoodSupply.Api.Health;
using FoodSupply.Api.Messaging;
using FoodSupply.Api.Repositories;
using FoodSupply.Api.Repositories.Interfaces;
using FoodSupply.Api.Security;
using FoodSupply.Api.SeedData;
using FoodSupply.Api.Services;
using FoodSupply.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();

builder.Services.AddControllers(options =>
    options.Filters.Add(new ResponseCacheAttribute
    {
        Location = ResponseCacheLocation.None,
        NoStore = true
    }))
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(allowIntegerValues: false)));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Paste only the access token. Swagger adds the Bearer prefix automatically."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHttpLogging(options =>
{
    // Log request metadata only; never log bodies, authorization headers, or refresh tokens.
    options.LoggingFields = HttpLoggingFields.RequestMethod |
                            HttpLoggingFields.RequestPath |
                            HttpLoggingFields.ResponseStatusCode;
});
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IAppCache>(_ => new AppCache(
    new MemoryCache(new MemoryCacheOptions
    {
        // A separate bounded instance prevents unrelated ASP.NET services from
        // needing to assign entry sizes in FoodSupply's application cache.
        SizeLimit = 4096,
        ExpirationScanFrequency = TimeSpan.FromMinutes(1)
    })));

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
{
    if (allowedOrigins.Length > 0)
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    }
    else
    {
        // Production must explicitly configure the browser app's origin.
        policy.SetIsOriginAllowed(_ => false)
            .AllowAnyHeader()
            .AllowAnyMethod();
    }
}));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/problem+json";
        await context.HttpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too Many Requests",
                Detail = "Too many authentication requests came from this address. Try again shortly.",
                Instance = context.HttpContext.Request.Path
            },
            cancellationToken);
    };
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 12,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services.AddAuthorization(options =>
{
    // Endpoints require a valid JWT unless they explicitly use [AllowAnonymous].
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var connectionString = builder.Configuration.GetConnectionString("FoodSupply");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "The FoodSupply connection string is missing. " +
        "Set ConnectionStrings:FoodSupply with .NET User Secrets.");
}

builder.Services.AddDbContext<FoodSupplyDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("postgres", tags: ["ready"])
    .AddCheck("liveness", () => HealthCheckResult.Healthy(), tags: ["live"]);

var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT settings are missing.");

if (string.IsNullOrWhiteSpace(jwtOptions.Issuer) ||
    string.IsNullOrWhiteSpace(jwtOptions.Audience) ||
    string.IsNullOrWhiteSpace(jwtOptions.SecretKey) ||
    Encoding.UTF8.GetByteCount(jwtOptions.SecretKey) < 32 ||
    jwtOptions.AccessTokenMinutes <= 0 ||
    jwtOptions.RefreshTokenDays <= 0)
{
    throw new InvalidOperationException(
        "JWT issuer, audience, access-token lifetime, and a secret key of at least 32 UTF-8 bytes must be configured. " +
        "Store the secret key in .NET User Secrets or a deployment secret store.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep JWT claim names such as "sub" and "role" unchanged.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,

            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),

            NameClaimType = "sub",
            RoleClaimType = "role"
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userIdValue = context.Principal?.FindFirstValue("sub");
                var sessionIdValue = context.Principal?.FindFirstValue("sid");

                if (!Guid.TryParse(userIdValue, out var userId) || userId == Guid.Empty ||
                    !Guid.TryParse(sessionIdValue, out var sessionId) || sessionId == Guid.Empty)
                {
                    context.Fail("The access token does not identify a valid login session.");
                    return;
                }

                var sessionRepository = context.HttpContext.RequestServices
                    .GetRequiredService<IUserSessionRepository>();
                var activeSession = await sessionRepository.IsActiveSessionAsync(
                    userId,
                    sessionId,
                    DateTime.UtcNow,
                    context.HttpContext.RequestAborted);

                if (!activeSession)
                {
                    context.Fail("The login session is expired or has been revoked.");
                }
            }
        };
    });

builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IRestaurantRepository, RestaurantRepository>();
builder.Services.AddScoped<IMenuItemRepository, MenuItemRepository>();
builder.Services.AddScoped<ICustomerAddressRepository, CustomerAddressRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderOutboxRepository, OrderOutboxRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserSessionRepository, UserSessionRepository>();
builder.Services.AddScoped<InitialAdministratorSeeder>();
builder.Services.AddScoped<DemoCatalogSeeder>();
builder.Services.AddScoped<IUserRoleApplicationRepository, UserRoleApplicationRepository>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IRestaurantService, RestaurantService>();
builder.Services.AddScoped<IMenuItemService, MenuItemService>();
builder.Services.AddScoped<ICustomerAddressService, CustomerAddressService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserRoleApplicationService, UserRoleApplicationService>();
builder.Services.AddScoped<IPasswordHasher<ApplicationUser>, PasswordHasher<ApplicationUser>>();
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration.GetSection(RabbitMqOptions.SectionName))
    .Validate(options =>
            !string.IsNullOrWhiteSpace(options.HostName) &&
            options.Port is > 0 and <= 65535 &&
            !string.IsNullOrWhiteSpace(options.VirtualHost) &&
            !string.IsNullOrWhiteSpace(options.UserName) &&
            !string.IsNullOrWhiteSpace(options.Password) &&
            (builder.Environment.IsDevelopment() ||
             (options.UserName != "guest" && options.Password != "guest")) &&
            !string.IsNullOrWhiteSpace(options.ExchangeName) &&
            !string.IsNullOrWhiteSpace(options.QueueName) &&
            !string.IsNullOrWhiteSpace(options.RoutingKey) &&
            options.PollIntervalSeconds > 0 &&
            options.BatchSize is > 0 and <= 100 &&
            options.MessageLockSeconds > 0 &&
            options.MaximumRetryDelaySeconds > 0,
        "RabbitMQ host, port, names, and positive retry/polling limits must be configured.")
    .ValidateOnStart();
builder.Services.AddHostedService<OrderOutboxPublisher>();
builder.Services.AddHostedService<OrderEventsConsumer>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseHttpLogging();
app.UseRateLimiter();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
}).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
}).AllowAnonymous();

await using (var scope = app.Services.CreateAsyncScope())
{
    var administratorSeeder = scope.ServiceProvider
        .GetRequiredService<InitialAdministratorSeeder>();
    await administratorSeeder.SeedIfConfiguredAsync();

    if (app.Environment.IsDevelopment())
    {
        var demoCatalogSeeder = scope.ServiceProvider
            .GetRequiredService<DemoCatalogSeeder>();
        await demoCatalogSeeder.SeedIfEmptyAsync();
    }
}

app.Run();
