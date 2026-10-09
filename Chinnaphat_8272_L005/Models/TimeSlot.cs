using System;
using System.Collections.Generic;

namespace Chinnaphat_8272_L005.Models;

/// <summary>
/// เธเนเธงเธเนเธงเธฅเธฒเธเธตเนเนเธเธดเธเนเธซเนเธเธญเธ (เธฃเธฒเธขเธเธฑเนเธงเนเธกเธ)
/// </summary>
public partial class TimeSlot
{
    /// <summary>
    /// PK เธฃเธซเธฑเธชเธเนเธงเธเนเธงเธฅเธฒ
    /// </summary>
    public byte SlotId { get; set; }

    /// <summary>
    /// เนเธงเธฅเธฒเนเธฃเธดเนเธก
    /// </summary>
    public TimeOnly StartTime { get; set; }

    /// <summary>
    /// เนเธงเธฅเธฒเธชเธดเนเธเธชเธธเธ
    /// </summary>
    public TimeOnly EndTime { get; set; }

    /// <summary>
    /// 1=เนเธเธดเธเนเธซเนเธเธญเธ
    /// </summary>
    public bool? IsActive { get; set; }

    public virtual ICollection<BookingSlot> BookingSlots { get; set; } = new List<BookingSlot>();

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual ICollection<WalkInQueue> WalkInQueues { get; set; } = new List<WalkInQueue>();
}
