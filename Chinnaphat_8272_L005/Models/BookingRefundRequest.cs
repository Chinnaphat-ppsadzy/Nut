namespace Chinnaphat_8272_L005.Models;

public class BookingRefundRequest
{
    public uint RequestId { get; set; }
    public uint? BookingId { get; set; }
    public uint? BookingItemId { get; set; }
    public uint? QueueId { get; set; }
    public uint? SaleId { get; set; }
    public uint? ProductId { get; set; }
    public uint? CustomerUserId { get; set; }
    public string ReferenceCode { get; set; } = string.Empty;
    public string? ProductName { get; set; }
    public uint? ProductQuantity { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public DateOnly ServiceDate { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "PENDING_SALES";
    public string? SalesNote { get; set; }
    public uint? SalesReviewedBy { get; set; }
    public DateTime? SalesReviewedAt { get; set; }
    public string? AccountingNote { get; set; }
    public string? RefundMethod { get; set; }
    public string? RefundReference { get; set; }
    public uint? ProcessedBy { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
