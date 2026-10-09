using System;
using System.Collections.Generic;

namespace Chinnaphat_8272_L005.Models;

/// <summary>
/// เธเธญเธฃเนเธ/เธงเธฑเธ/เธเนเธงเธเนเธงเธฅเธฒเธเธตเนเธเธนเธเธเธญเธ (เนเธเนเนเธชเธเธเธชเธตเธงเนเธฒเธ-เนเธกเนเธงเนเธฒเธ)
/// </summary>
public partial class BookingSlot
{
    /// <summary>
    /// PK
    /// </summary>
    public uint BookingSlotId { get; set; }

    /// <summary>
    /// FK -&gt; bookings.booking_id
    /// </summary>
    public uint BookingId { get; set; }

    /// <summary>
    /// FK -&gt; courts.court_id
    /// </summary>
    public ushort CourtId { get; set; }

    /// <summary>
    /// FK -&gt; time_slots.slot_id
    /// </summary>
    public byte SlotId { get; set; }

    /// <summary>
    /// เธงเธฑเธเธเธตเนเนเธฅเนเธ
    /// </summary>
    public DateOnly PlayDate { get; set; }

    /// <summary>
    /// เธฃเธฒเธเธฒเธเธญเธฃเนเธ เธ เธเธญเธเธเธญเธ (snapshot)
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// เธชเนเธงเธเธฅเธเธเธญเธเธเนเธงเธเนเธงเธฅเธฒเธเธตเน (เนเธเนเธ เนเธเธฃเธฏ 1)
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// 1=เธเธณเธฅเธฑเธเธเธญเธเธญเธขเธนเน / NULL=เธเธฅเนเธญเธขเธเธทเธเนเธฅเนเธง (เนเธเนเธฃเนเธงเธก UNIQUE เธเธฑเธเธเธญเธเธเนเธณ)
    /// </summary>
    public sbyte? LockKey { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual Court Court { get; set; } = null!;

    public virtual TimeSlot Slot { get; set; } = null!;
}
