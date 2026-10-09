using System;
using System.Collections.Generic;

namespace Chinnaphat_8272_L005.Models;

/// <summary>
/// เธซเธฑเธงเธเธดเธฅเธเธฒเธฃเธเธญเธ
/// </summary>
public partial class Booking
{
    /// <summary>
    /// PK
    /// </summary>
    public uint BookingId { get; set; }

    /// <summary>
    /// เนเธฅเธเธเธตเนเธเธฒเธฃเธเธญเธเธเธตเนเนเธชเธเธเนเธซเนเธฅเธนเธเธเนเธฒ เนเธเนเธ BK20261004001
    /// </summary>
    public string BookingCode { get; set; } = null!;

    /// <summary>
    /// FK -&gt; users.user_id เธฅเธนเธเธเนเธฒ
    /// </summary>
    public uint UserId { get; set; }

    /// <summary>
    /// เธงเธฑเธเธเธตเนเนเธเนเธฒเนเธเนเธชเธเธฒเธก
    /// </summary>
    public DateOnly PlayDate { get; set; }

    /// <summary>
    /// เธเธดเธฅเธเธตเนเนเธเนเธเธเธฒเธฃเธเธญเธเธเธฃเธฑเนเธเนเธฃเธเธเธญเธเธชเธกเธฒเธเธดเธเธซเธฃเธทเธญเนเธกเน (snapshot เนเธเนเธเธฑเธเนเธเธฃเธฏ 5)
    /// </summary>
    public bool IsFirstBooking { get; set; }

    /// <summary>
    /// เธฃเธงเธกเธเนเธฒเนเธเนเธฒเธเธญเธฃเนเธ (เธเนเธญเธเธฅเธ)
    /// </summary>
    public decimal CourtAmount { get; set; }

    /// <summary>
    /// เธฃเธงเธกเธเนเธฒเธชเธดเธเธเนเธฒเธเธตเนเธเธทเนเธญ (เธเนเธญเธเธฅเธ)
    /// </summary>
    public decimal SaleAmount { get; set; }

    /// <summary>
    /// เธฃเธงเธกเธเนเธฒเนเธเนเธฒเธญเธธเธเธเธฃเธเน (เธเนเธญเธเธฅเธ)
    /// </summary>
    public decimal RentalAmount { get; set; }

    /// <summary>
    /// เธชเนเธงเธเธฅเธเธฃเธงเธกเธเธฒเธเนเธเธฃเนเธกเธเธฑเนเธ
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// เธขเธญเธเธชเธธเธเธเธดเธเธตเนเธเนเธญเธเธเธณเธฃเธฐ
    /// </summary>
    public decimal NetAmount { get; set; }

    /// <summary>
    /// เธฃเธญเธเธณเธฃเธฐ/เธฃเธญเธเธฃเธงเธเธชเธฅเธดเธ/เธขเธทเธเธขเธฑเธเนเธฅเนเธง/เนเธเนเธฒเนเธเนเธเธฒเธ/เนเธชเธฃเนเธเธชเธดเนเธ/เธขเธเนเธฅเธดเธ/เธชเธฅเธดเธเนเธกเนเธเนเธฒเธ/เนเธกเนเธกเธฒ/เธซเธกเธเนเธงเธฅเธฒ
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// เนเธงเธฅเธฒเธซเธกเธเธญเธฒเธขเธธเธเธฒเธฃเธเธณเธฃเธฐเนเธเธดเธ (เธเธฅเนเธญเธขเธเธญเธฃเนเธเธเธทเธเธญเธฑเธเนเธเธกเธฑเธเธด)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// เนเธงเธฅเธฒ Check-in
    /// </summary>
    public DateTime? CheckedInAt { get; set; }

    /// <summary>
    /// FK -&gt; users (เธเนเธฒเธขเธเธนเนเธฅเธเธญเธฃเนเธเธเธตเน Check-in)
    /// </summary>
    public uint? CheckedInBy { get; set; }

    /// <summary>
    /// เนเธงเธฅเธฒ Check-out
    /// </summary>
    public DateTime? CheckedOutAt { get; set; }

    /// <summary>
    /// FK -&gt; users (เธเนเธฒเธขเธเธนเนเธฅเธเธญเธฃเนเธเธเธตเน Check-out)
    /// </summary>
    public uint? CheckedOutBy { get; set; }

    /// <summary>
    /// เนเธงเธฅเธฒเธขเธเนเธฅเธดเธ
    /// </summary>
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// เนเธซเธเธธเธเธฅเธขเธเนเธฅเธดเธ
    /// </summary>
    public string? CancelReason { get; set; }

    /// <summary>
    /// เธงเธฑเธเธเธตเนเธชเธฃเนเธฒเธเธเธดเธฅ
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// เธงเธฑเธเธเธตเนเธเธฑเธเธเธตเธญเธเธธเธกเธฑเธเธด
    /// </summary>
    public DateTime? ConfirmedAt { get; set; }

    /// <summary>
    /// เนเธเนเนเธเธฅเนเธฒเธชเธธเธ
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<BookingItem> BookingItems { get; set; } = new List<BookingItem>();

    public virtual ICollection<BookingPromotion> BookingPromotions { get; set; } = new List<BookingPromotion>();

    public virtual ICollection<BookingSlot> BookingSlots { get; set; } = new List<BookingSlot>();

    public virtual User? CheckedInByNavigation { get; set; }

    public virtual User? CheckedOutByNavigation { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Receipt> Receipts { get; set; } = new List<Receipt>();

    public virtual ICollection<Refund> Refunds { get; set; } = new List<Refund>();

    public virtual User User { get; set; } = null!;
}
