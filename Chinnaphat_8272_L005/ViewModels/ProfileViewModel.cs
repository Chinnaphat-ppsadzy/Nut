using System;
using System.Collections.Generic;

namespace Chinnaphat_8272_L005.ViewModels;

public class ProfileViewModel
{
    // User info
    public uint UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string RoleName { get; set; } = "สมาชิก";
    public string RoleCode { get; set; } = "CUSTOMER";
    public string Status { get; set; } = "ACTIVE";
    public DateTime CreatedAt { get; set; }

    // Summary Metrics
    public int TotalBookingsCount { get; set; }
    public decimal TotalSpending { get; set; }
    public int TotalRentalItemsCount { get; set; }
    public int TotalSaleItemsCount { get; set; }

    // 1. ประวัติการจองคอร์ท (Court Booking History)
    public List<ProfileBookingItem> CourtBookings { get; set; } = new();

    // 2. ประวัติการสั่งซื้อสินค้า (Product Purchase History)
    public List<ProfileOrderItem> PurchaseHistory { get; set; } = new();

    // 3. ประวัติการเช่าอุปกรณ์ (Equipment Rental History)
    public List<ProfileRentalItem> RentalHistory { get; set; } = new();
}

public class ProfileBookingItem
{
    public uint BookingId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public string PlayDate { get; set; } = string.Empty;
    public string CourtNames { get; set; } = string.Empty;
    public string TimeSlots { get; set; } = string.Empty;
    public decimal CourtAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string Status { get; set; } = "PENDING";
    public string StatusText { get; set; } = string.Empty;
    public string StatusBadgeClass { get; set; } = "bg-warning text-dark";
    public string? SlipImageUrl { get; set; }
    public string? TaxInvoiceUrl { get; set; }
    public string? TaxInvoiceNo { get; set; }
    public uint? ReceiptId { get; set; }
    public bool HasTaxInvoice { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ProfileOrderItem
{
    public uint BookingItemId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public uint Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public string ItemStatus { get; set; } = "RESERVED";
    public string ItemStatusText { get; set; } = string.Empty;
    public string ItemStatusBadgeClass { get; set; } = "bg-secondary";
    public DateTime OrderDate { get; set; }
    public uint? RefundRequestId { get; set; }
    public string? RefundRequestStatus { get; set; }
    public string? RefundRequestReason { get; set; }
    public string? RefundRequestReviewNote { get; set; }
    public bool CanRequestRefund { get; set; }
}

public class ProfileRentalItem
{
    public uint BookingItemId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public uint Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Deposit { get; set; }
    public decimal LineTotal { get; set; }
    public string ItemStatus { get; set; } = "RESERVED";
    public string ItemStatusText { get; set; } = "รอรับอุปกรณ์";
    public string ItemStatusBadgeClass { get; set; } = "bg-warning text-dark";
    public DateTime? HandedOverAt { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public DateTime RentalDate { get; set; }
}
