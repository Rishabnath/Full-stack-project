namespace FoodSupply.Api.Entities;

public enum OrderStatus
{
    Placed = 1,
    Accepted = 2,
    Preparing = 3,
    Packed = 4,
    PickedUp = 5,
    Delivered = 6,
    Rejected = 7,
    Cancelled = 8
}
