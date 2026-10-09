using System;
using System.Collections.Generic;

namespace Chinnaphat_8272_L005.Models;

/// <summary>
/// เธเธดเธงเธฅเธนเธเธเนเธฒ Walk-in
/// </summary>
public partial class WalkInQueue
{
    /// <summary>
    /// PK
    /// </summary>
    public uint QueueId { get; set; }

    /// <summary>
    /// FK -&gt; courts.court_id (เธเธญเธฃเนเธเธเธตเนเธเธฑเธเนเธซเน)
    /// </summary>
    public ushort? CourtId { get; set; }

    /// <summary>
    /// เธเธทเนเธญเธฅเธนเธเธเนเธฒ Walk-in
    /// </summary>
    public string CustomerName { get; set; } = null!;

    /// <summary>
    /// เนเธเธญเธฃเนเนเธเธฃ
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// เธงเธฑเธเธเธตเน
    /// </summary>
    public DateOnly QueueDate { get; set; }

    /// <summary>
    /// FK -&gt; time_slots.slot_id
    /// </summary>
    public byte? SlotId { get; set; }

    /// <summary>
    /// เธฅเธณเธเธฑเธเธเธดเธงเธเธญเธเธงเธฑเธ
    /// </summary>
    public ushort QueueNo { get; set; }

    /// <summary>
    /// เธชเธเธฒเธเธฐเธเธดเธง
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// FK -&gt; users.user_id (เธเนเธฒเธขเธเธนเนเธฅเธเธญเธฃเนเธ)
    /// </summary>
    public uint HandledBy { get; set; }

    /// <summary>
    /// เนเธงเธฅเธฒเนเธเนเธฒเธเธดเธง
    /// </summary>
    public DateTime CreatedAt { get; set; }

    public virtual Court? Court { get; set; }

    public virtual User HandledByNavigation { get; set; } = null!;

    public virtual TimeSlot? Slot { get; set; }
}
