namespace FoodSupply.Api.Entities;

public sealed class ApplicationUser
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    // Store only a password hash here, never a plain-text password.
    public string PasswordHash { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public UserRole Role { get; set; } = UserRole.Customer;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<CustomerAddress> Addresses { get; set; } = new List<CustomerAddress>();

    public ICollection<Restaurant> OwnedRestaurants { get; set; } = new List<Restaurant>();

    public ICollection<Order> Orders { get; set; } = new List<Order>();

    public ICollection<OrderStatusHistory> StatusChanges { get; set; } = new List<OrderStatusHistory>();

    public ICollection<UserRoleApplication> RoleApplications { get; set; } =
        new List<UserRoleApplication>();

    public ICollection<UserRoleApplication> ReviewedRoleApplications { get; set; } =
        new List<UserRoleApplication>();

    public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
}
