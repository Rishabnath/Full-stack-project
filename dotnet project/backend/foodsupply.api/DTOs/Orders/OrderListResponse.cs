namespace FoodSupply.Api.DTOs.Orders;

public sealed class OrderListResponse
{
    public IReadOnlyList<OrderSummaryResponse> Items { get; set; } =
        Array.Empty<OrderSummaryResponse>();

    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }

    public int TotalPages =>
        PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
