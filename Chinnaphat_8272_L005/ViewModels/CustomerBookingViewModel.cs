using System;
using System.Collections.Generic;
using Chinnaphat_8272_L005.Models;

namespace Chinnaphat_8272_L005.ViewModels;

public class CustomerBookingViewModel
{
    // สนามและช่วงเวลา
    public List<Court> Courts { get; set; } = new();
    public List<TimeSlot> TimeSlots { get; set; } = new();
    public string SelectedDate { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");

    // สินค้าและอุปกรณ์
    public List<Product> Products { get; set; } = new();

    // โปรโมชั่นที่ใช้งานอยู่
    public List<Promotion> ActivePromotions { get; set; } = new();

    // ประวัติการจองและติดตามสถานะ
    public List<BookingTrackItem> MyBookings { get; set; } = new();
}

public class BookingTrackItem
{
    public uint BookingId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public string PlayDate { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public decimal CourtAmount { get; set; }
    public decimal SaleAmount { get; set; }
    public decimal RentalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string Status { get; set; } = "PENDING_VERIFICATION";
    public string StatusText { get; set; } = "รอตรวจสอบการชำระเงิน";
    public string StatusBadgeClass { get; set; } = "bg-warning text-dark";
    public string? SlipImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<string> CourtSlots { get; set; } = new();
    public List<string> ItemsSummary { get; set; } = new();
    public List<string> AppliedPromos { get; set; } = new();
}
