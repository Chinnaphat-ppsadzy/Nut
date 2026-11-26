namespace Chinnaphat_8272_L005.ViewModels;

/// <summary>Unified tracking row for product refunds and rental deposit returns on the sales page.</summary>
public class SalesRefundTrackingItem
{
    public string RequestType { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string ReferenceCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Amount { get; set; }
    public string AmountLabel { get; set; } = "ยอดคืน";
    public string? AmountDetail { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public string StatusClass { get; set; } = string.Empty;
    public string? SalesNote { get; set; }
    public string? AccountingNote { get; set; }
    public string? RefundMethod { get; set; }
    public string? RefundReference { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
