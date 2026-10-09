using System;
using System.Collections.Generic;

namespace Chinnaphat_8272_L005.Models;

/// <summary>
/// เธเธฒเธฃเธเธทเธเนเธเธดเธเธฅเธนเธเธเนเธฒ
/// </summary>
public partial class Refund
{
    /// <summary>
    /// PK
    /// </summary>
    public uint RefundId { get; set; }

    /// <summary>
    /// FK -&gt; bookings.booking_id
    /// </summary>
    public uint BookingId { get; set; }

    /// <summary>
    /// FK -&gt; payments.payment_id
    /// </summary>
    public uint PaymentId { get; set; }

    /// <summary>
    /// เธขเธญเธเธเธทเธเนเธเธดเธ
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// เนเธซเธเธธเธเธฅ เนเธเนเธ เธฅเธนเธเธเนเธฒเธขเธเนเธฅเธดเธเธเธฒเธกเนเธเธทเนเธญเธเนเธ
    /// </summary>
    public string Reason { get; set; } = null!;

    /// <summary>
    /// เธชเธเธฒเธเธฐเธเธฒเธฃเธเธทเธเนเธเธดเธ
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// FK -&gt; users (เธเนเธฒเธขเธเธฑเธเธเธต)
    /// </summary>
    public uint? ProcessedBy { get; set; }

    /// <summary>
    /// เธงเธฑเธเธเธตเนเธเธณเนเธเธดเธเธเธฒเธฃ
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// เธงเธฑเธเธเธตเนเธเธญเธเธทเธเนเธเธดเธ
    /// </summary>
    public DateTime CreatedAt { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual Payment Payment { get; set; } = null!;

    public virtual User? ProcessedByNavigation { get; set; }
}
