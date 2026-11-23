namespace Chinnaphat_8272_L005.Models;

public partial class PosSaleTransaction
{
    public uint SaleId { get; set; }
    public string ReceiptCode { get; set; } = null!;
    public string CustomerName { get; set; } = null!;
    public string? CustomerPhone { get; set; }
    public string ItemsSummary { get; set; } = null!;
    public string? ItemsJson { get; set; }
    public decimal SaleAmount { get; set; }
    public decimal RentalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetTotal { get; set; }
    public string PaymentMethod { get; set; } = "QR";
    public string PaymentStatus { get; set; } = "PENDING_VERIFY";
    public DateTime? PaidAt { get; set; }
    public string? SlipImageUrl { get; set; }
    public uint? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? RejectReason { get; set; }
    public uint CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}
