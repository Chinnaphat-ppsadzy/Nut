using System;
using System.Collections.Generic;

namespace Chinnaphat_8272_L005.Models;

/// <summary>
/// เธเธฒเธฃเนเธเนเธเนเธญเธเธเธณเธฃเธฐเนเธเธดเธเนเธฅเธฐเธเธฅเธเธฃเธงเธเธชเธฅเธดเธ
/// </summary>
public partial class Payment
{
    /// <summary>
    /// PK
    /// </summary>
    public uint PaymentId { get; set; }

    /// <summary>
    /// FK -&gt; bookings.booking_id
    /// </summary>
    public uint BookingId { get; set; }

    /// <summary>
    /// เธขเธญเธเธเธตเนเธฅเธนเธเธเนเธฒเนเธเนเธเนเธญเธ
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// เธงเธฑเธ-เนเธงเธฅเธฒเธเธตเนเนเธญเธ
    /// </summary>
    public DateTime? TransferAt { get; set; }

    /// <summary>
    /// เธเธตเนเธญเธขเธนเนเนเธเธฅเนเธชเธฅเธดเธ
    /// </summary>
    public string SlipImageUrl { get; set; } = null!;

    /// <summary>
    /// เธฃเธญเธเธฃเธงเธ/เธญเธเธธเธกเธฑเธเธด/เนเธกเนเธญเธเธธเธกเธฑเธเธด
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// FK -&gt; users (เธเนเธฒเธขเธเธฑเธเธเธตเธเธนเนเธเธฃเธงเธ)
    /// </summary>
    public uint? VerifiedBy { get; set; }

    /// <summary>
    /// เนเธงเธฅเธฒเธเธตเนเธเธฃเธงเธ
    /// </summary>
    public DateTime? VerifiedAt { get; set; }

    /// <summary>
    /// เนเธซเธเธธเธเธฅเธเธตเนเนเธกเนเธญเธเธธเธกเธฑเธเธด
    /// </summary>
    public string? RejectReason { get; set; }

    /// <summary>
    /// เธงเธฑเธเธเธตเนเธญเธฑเธเนเธซเธฅเธเธชเธฅเธดเธ
    /// </summary>
    public DateTime CreatedAt { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual ICollection<Receipt> Receipts { get; set; } = new List<Receipt>();

    public virtual ICollection<Refund> Refunds { get; set; } = new List<Refund>();

    public virtual User? VerifiedByNavigation { get; set; }
}
