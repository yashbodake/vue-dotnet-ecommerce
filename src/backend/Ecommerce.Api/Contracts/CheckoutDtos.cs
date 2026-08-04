using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Api.Contracts;

/// <summary>
/// Available shipping option returned by the checkout API.
/// </summary>
public sealed class ShippingOption
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string EstimatedDays { get; set; } = string.Empty;
}

/// <summary>
/// Place order request. Card fields are demo-validated only and are never persisted.
/// </summary>
public sealed class PlaceOrderRequest
{
    [Required]
    public string ShippingAddress { get; set; } = string.Empty;

    [Required]
    public string ShippingMethod { get; set; } = string.Empty;

    [Required]
    public string CardName { get; set; } = string.Empty;

    [Required]
    public string CardNumber { get; set; } = string.Empty;

    [Required]
    public string CardExpiry { get; set; } = string.Empty;

    [Required]
    public string CardCvv { get; set; } = string.Empty;
}

/// <summary>
/// Single order line returned in order confirmations / detail.
/// </summary>
public sealed class OrderItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? VariantName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

/// <summary>
/// Order confirmation / detail response.
/// </summary>
public sealed class OrderConfirmationDto
{
    public int OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string ShippingMethod { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public IReadOnlyList<OrderItemDto> Items { get; set; } = [];
}
