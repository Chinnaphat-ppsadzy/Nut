namespace Chinnaphat_8272_L005.Models;

/// <summary>คำขอคืนมัดจำอุปกรณ์เช่าจากทั้งรายการจองและ POS</summary>
public class RentalDepositRefundRequest
{
    public uint RequestId { get; set; }
    public uint? BookingItemId { get; set; }
    public uint? SaleId { get; set; }
    public uint ProductId { get; set; }
    public string ReceiptCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string? RefundDestination { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal DepositAmount { get; set; }
    public string? DamageDescription { get; set; }
    public string? DamagePhotoUrl { get; set; }
    public string? DamageAssessmentStatus { get; set; }
    public uint? DamageAssessedBy { get; set; }
    public DateTime? DamageAssessedAt { get; set; }
    public decimal FineAmount { get; set; }
    public decimal? RefundedAmount { get; set; }
    public string? RefundMethod { get; set; }
    public string? RefundReference { get; set; }
    public string Status { get; set; } = "PENDING";
    public uint RequestedBy { get; set; }
    public uint? ProcessedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
