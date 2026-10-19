namespace FoodSupply.Api.Caching;

public static class CacheTags
{
    public const string Categories = "catalog:categories";
    public const string Restaurants = "catalog:restaurants";
    public const string MenuItems = "catalog:menu-items";
    public const string Orders = "orders";
    public const string RoleApplications = "role-applications";
    public const string Addresses = "addresses";
    public const string Sessions = "sessions";
}

public static class CacheDurations
{
    public static readonly TimeSpan Catalog = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan UserData = TimeSpan.FromSeconds(15);
}
