namespace Ecommerce.Api.Contracts;

/// <summary>
/// Summary of an order returned by the account order history endpoint.
/// </summary>
public sealed class OrderSummaryDto
{
    public int OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
}
