using FoodSupply.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodSupply.Api.Data;

public sealed class FoodSupplyDbContext : DbContext
{
    public FoodSupplyDbContext(DbContextOptions<FoodSupplyDbContext> options)
        : base(options)
    {
    }

    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Restaurant> Restaurants => Set<Restaurant>();

    public DbSet<MenuItem> MenuItems => Set<MenuItem>();

    public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<OrderStatusHistory> OrderStatusHistory => Set<OrderStatusHistory>();

    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("app_users");

            entity.HasKey(user => user.Id);

            entity.Property(user => user.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(user => user.FirstName)
                .HasColumnName("first_name")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(user => user.LastName)
                .HasColumnName("last_name")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(user => user.Email)
                .HasColumnName("email")
                .HasMaxLength(320)
                .IsRequired();

            entity.HasIndex(user => user.Email)
                .IsUnique();

            entity.Property(user => user.PasswordHash)
                .HasColumnName("password_hash")
                .HasMaxLength(512)
                .IsRequired();

            entity.Property(user => user.PhoneNumber)
                .HasColumnName("phone_number")
                .HasMaxLength(30);

            entity.Property(user => user.Role)
                .HasColumnName("role")
                .HasConversion<string>()
                .HasMaxLength(32)
                .HasDefaultValue(UserRole.Customer)
                .HasSentinel(UserRole.Customer)
                .IsRequired();

            entity.Property(user => user.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true)
                .IsRequired();

            entity.Property(user => user.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");

            entity.HasKey(category => category.Id);

            entity.Property(category => category.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(category => category.Name)
                .HasColumnName("name")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(category => category.Description)
                .HasColumnName("description")
                .HasMaxLength(300);

            entity.Property(category => category.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true)
                .IsRequired();

            entity.HasIndex(category => category.Name)
                .IsUnique();
        });

        modelBuilder.Entity<Restaurant>(entity =>
        {
            entity.ToTable("restaurants");

            entity.HasKey(restaurant => restaurant.Id);

            entity.Property(restaurant => restaurant.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(restaurant => restaurant.Name)
                .HasColumnName("name")
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(restaurant => restaurant.Description)
                .HasColumnName("description")
                .HasMaxLength(500);

            entity.Property(restaurant => restaurant.Address)
                .HasColumnName("address")
                .HasMaxLength(250)
                .IsRequired();

            entity.Property(restaurant => restaurant.City)
                .HasColumnName("city")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(restaurant => restaurant.PhoneNumber)
                .HasColumnName("phone_number")
                .HasMaxLength(30);

            entity.Property(restaurant => restaurant.ImageUrl)
                .HasColumnName("image_url")
                .HasMaxLength(2048);

            entity.Property(restaurant => restaurant.OwnerId)
                .HasColumnName("owner_id");

            entity.Property(restaurant => restaurant.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true)
                .IsRequired();

            entity.Property(restaurant => restaurant.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            entity.HasIndex(restaurant => new { restaurant.City, restaurant.IsActive });

            entity.HasOne(restaurant => restaurant.Owner)
                .WithMany(user => user.OwnedRestaurants)
                .HasForeignKey(restaurant => restaurant.OwnerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<MenuItem>(entity =>
        {
            entity.ToTable("menu_items", table =>
                table.HasCheckConstraint("ck_menu_items_price_positive", "price > 0"));

            entity.HasKey(menuItem => menuItem.Id);

            entity.Property(menuItem => menuItem.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(menuItem => menuItem.RestaurantId)
                .HasColumnName("restaurant_id");

            entity.Property(menuItem => menuItem.CategoryId)
                .HasColumnName("category_id");

            entity.Property(menuItem => menuItem.Name)
                .HasColumnName("name")
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(menuItem => menuItem.Description)
                .HasColumnName("description")
                .HasMaxLength(1000);

            entity.Property(menuItem => menuItem.Price)
                .HasColumnName("price")
                .HasPrecision(12, 2)
                .IsRequired();

            entity.Property(menuItem => menuItem.ImageUrl)
                .HasColumnName("image_url")
                .HasMaxLength(2048);

            entity.Property(menuItem => menuItem.IsAvailable)
                .HasColumnName("is_available")
                .HasDefaultValue(true)
                .IsRequired();

            entity.Property(menuItem => menuItem.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            entity.HasIndex(menuItem => new { menuItem.RestaurantId, menuItem.IsAvailable });
            entity.HasIndex(menuItem => menuItem.CategoryId);

            entity.HasOne(menuItem => menuItem.Restaurant)
                .WithMany(restaurant => restaurant.MenuItems)
                .HasForeignKey(menuItem => menuItem.RestaurantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(menuItem => menuItem.Category)
                .WithMany(category => category.MenuItems)
                .HasForeignKey(menuItem => menuItem.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CustomerAddress>(entity =>
        {
            entity.ToTable("customer_addresses");

            entity.HasKey(address => address.Id);

            entity.Property(address => address.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(address => address.UserId)
                .HasColumnName("user_id");

            entity.Property(address => address.Label)
                .HasColumnName("label")
                .HasMaxLength(60)
                .IsRequired();

            entity.Property(address => address.Line1)
                .HasColumnName("line1")
                .HasMaxLength(250)
                .IsRequired();

            entity.Property(address => address.Line2)
                .HasColumnName("line2")
                .HasMaxLength(250);

            entity.Property(address => address.City)
                .HasColumnName("city")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(address => address.State)
                .HasColumnName("state")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(address => address.PostalCode)
                .HasColumnName("postal_code")
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(address => address.Country)
                .HasColumnName("country")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(address => address.IsDefault)
                .HasColumnName("is_default")
                .HasDefaultValue(false)
                .IsRequired();

            entity.Property(address => address.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            entity.HasIndex(address => address.UserId);

            entity.HasOne(address => address.User)
                .WithMany(user => user.Addresses)
                .HasForeignKey(address => address.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders", table =>
                table.HasCheckConstraint(
                    "ck_orders_amounts_nonnegative",
                    "subtotal >= 0 AND delivery_fee >= 0 AND tax_amount >= 0 AND total_amount >= 0"));

            entity.HasKey(order => order.Id);

            entity.Property(order => order.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(order => order.OrderNumber)
                .HasColumnName("order_number")
                .HasMaxLength(40)
                .IsRequired();

            entity.HasIndex(order => order.OrderNumber)
                .IsUnique();

            entity.Property(order => order.CustomerId)
                .HasColumnName("customer_id");

            entity.Property(order => order.RestaurantId)
                .HasColumnName("restaurant_id");

            entity.Property(order => order.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(32)
                .HasDefaultValue(OrderStatus.Placed)
                .HasSentinel(OrderStatus.Placed)
                .IsRequired();

            entity.Property(order => order.Subtotal)
                .HasColumnName("subtotal")
                .HasPrecision(12, 2)
                .HasDefaultValue(0m)
                .IsRequired();

            entity.Property(order => order.DeliveryFee)
                .HasColumnName("delivery_fee")
                .HasPrecision(12, 2)
                .HasDefaultValue(0m)
                .IsRequired();

            entity.Property(order => order.TaxAmount)
                .HasColumnName("tax_amount")
                .HasPrecision(12, 2)
                .HasDefaultValue(0m)
                .IsRequired();

            entity.Property(order => order.TotalAmount)
                .HasColumnName("total_amount")
                .HasPrecision(12, 2)
                .HasDefaultValue(0m)
                .IsRequired();

            entity.Property(order => order.CurrencyCode)
                .HasColumnName("currency_code")
                .HasMaxLength(3)
                .HasDefaultValue("INR")
                .IsRequired();

            entity.Property(order => order.RecipientName)
                .HasColumnName("recipient_name")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(order => order.RecipientPhoneNumber)
                .HasColumnName("recipient_phone_number")
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(order => order.DeliveryAddressLine1)
                .HasColumnName("delivery_address_line1")
                .HasMaxLength(250)
                .IsRequired();

            entity.Property(order => order.DeliveryAddressLine2)
                .HasColumnName("delivery_address_line2")
                .HasMaxLength(250);

            entity.Property(order => order.DeliveryCity)
                .HasColumnName("delivery_city")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(order => order.DeliveryState)
                .HasColumnName("delivery_state")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(order => order.DeliveryPostalCode)
                .HasColumnName("delivery_postal_code")
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(order => order.DeliveryCountry)
                .HasColumnName("delivery_country")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(order => order.CustomerNote)
                .HasColumnName("customer_note")
                .HasMaxLength(500);

            entity.Property(order => order.PlacedAtUtc)
                .HasColumnName("placed_at_utc")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            entity.Property(order => order.UpdatedAtUtc)
                .HasColumnName("updated_at_utc")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            entity.HasIndex(order => new { order.CustomerId, order.PlacedAtUtc });
            entity.HasIndex(order => new { order.RestaurantId, order.Status });

            entity.HasOne(order => order.Customer)
                .WithMany(user => user.Orders)
                .HasForeignKey(order => order.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(order => order.Restaurant)
                .WithMany(restaurant => restaurant.Orders)
                .HasForeignKey(order => order.RestaurantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("order_items", table =>
                table.HasCheckConstraint("ck_order_items_quantity_positive", "quantity > 0"));

            entity.HasKey(orderItem => orderItem.Id);

            entity.Property(orderItem => orderItem.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(orderItem => orderItem.OrderId)
                .HasColumnName("order_id");

            entity.Property(orderItem => orderItem.MenuItemId)
                .HasColumnName("menu_item_id");

            entity.Property(orderItem => orderItem.ItemName)
                .HasColumnName("item_name")
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(orderItem => orderItem.UnitPrice)
                .HasColumnName("unit_price")
                .HasPrecision(12, 2)
                .IsRequired();

            entity.Property(orderItem => orderItem.Quantity)
                .HasColumnName("quantity")
                .IsRequired();

            entity.Property(orderItem => orderItem.SpecialInstructions)
                .HasColumnName("special_instructions")
                .HasMaxLength(500);

            entity.HasIndex(orderItem => orderItem.OrderId);
            entity.HasIndex(orderItem => orderItem.MenuItemId);

            entity.HasOne(orderItem => orderItem.Order)
                .WithMany(order => order.Items)
                .HasForeignKey(orderItem => orderItem.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(orderItem => orderItem.MenuItem)
                .WithMany(menuItem => menuItem.OrderItems)
                .HasForeignKey(orderItem => orderItem.MenuItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderStatusHistory>(entity =>
        {
            entity.ToTable("order_status_history");

            entity.HasKey(history => history.Id);

            entity.Property(history => history.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(history => history.OrderId)
                .HasColumnName("order_id");

            entity.Property(history => history.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();

            entity.Property(history => history.ChangedAtUtc)
                .HasColumnName("changed_at_utc")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            entity.Property(history => history.Note)
                .HasColumnName("note")
                .HasMaxLength(500);

            entity.Property(history => history.ChangedByUserId)
                .HasColumnName("changed_by_user_id");

            entity.HasIndex(history => new { history.OrderId, history.ChangedAtUtc });

            entity.HasOne(history => history.Order)
                .WithMany(order => order.StatusHistory)
                .HasForeignKey(history => history.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(history => history.ChangedByUser)
                .WithMany(user => user.StatusChanges)
                .HasForeignKey(history => history.ChangedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("payments");

            entity.HasKey(payment => payment.Id);

            entity.Property(payment => payment.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(payment => payment.OrderId)
                .HasColumnName("order_id");

            entity.Property(payment => payment.Method)
                .HasColumnName("method")
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();

            entity.Property(payment => payment.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(32)
                .HasDefaultValue(PaymentStatus.Pending)
                .HasSentinel(PaymentStatus.Pending)
                .IsRequired();

            entity.Property(payment => payment.Amount)
                .HasColumnName("amount")
                .HasPrecision(12, 2)
                .IsRequired();

            entity.Property(payment => payment.CurrencyCode)
                .HasColumnName("currency_code")
                .HasMaxLength(3)
                .HasDefaultValue("INR")
                .IsRequired();

            entity.Property(payment => payment.ProviderReference)
                .HasColumnName("provider_reference")
                .HasMaxLength(200);

            entity.Property(payment => payment.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            entity.Property(payment => payment.PaidAtUtc)
                .HasColumnName("paid_at_utc")
                .HasColumnType("timestamp with time zone");

            entity.HasIndex(payment => new { payment.OrderId, payment.Status });

            entity.HasOne(payment => payment.Order)
                .WithMany(order => order.Payments)
                .HasForeignKey(payment => payment.OrderId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
