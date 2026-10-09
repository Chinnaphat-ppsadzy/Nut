using System;
using System.Collections.Generic;

namespace Chinnaphat_8272_L005.Models;

/// <summary>
/// เนเธเธฃเนเธกเธเธฑเนเธเธเธตเนเธเธนเธเนเธเนเธเธฃเธดเธเนเธเนเธเนเธฅเธฐเธเธดเธฅ
/// </summary>
public partial class BookingPromotion
{
    /// <summary>
    /// PK
    /// </summary>
    public uint BookingPromotionId { get; set; }

    /// <summary>
    /// FK -&gt; bookings.booking_id
    /// </summary>
    public uint BookingId { get; set; }

    /// <summary>
    /// FK -&gt; promotions.promotion_id
    /// </summary>
    public uint PromotionId { get; set; }

    /// <summary>
    /// เธชเนเธงเธเธฅเธเธเธตเนเนเธเนเธเธฒเธเนเธเธฃเธฏ เธเธตเนเนเธเธเธดเธฅเธเธตเน
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// เธฃเธฒเธขเธฅเธฐเนเธญเธตเธขเธเธเธตเนเนเธชเธเธเธฅเธนเธเธเนเธฒ (snapshot)
    /// </summary>
    public string? Detail { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual Promotion Promotion { get; set; } = null!;
}
