using System;
using System.Collections.Generic;

namespace Chinnaphat_8272_L005.Models;

/// <summary>
/// เธเนเธญเธกเธนเธฅเธเธญเธฃเนเธเนเธเธเธกเธดเธเธเธฑเธ
/// </summary>
public partial class Court
{
    /// <summary>
    /// PK เธฃเธซเธฑเธชเธเธญเธฃเนเธ
    /// </summary>
    public ushort CourtId { get; set; }

    /// <summary>
    /// เธเธทเนเธญเธเธญเธฃเนเธ เนเธเนเธ Court 1
    /// </summary>
    public string CourtName { get; set; } = null!;

    /// <summary>
    /// เธฃเธฒเธขเธฅเธฐเนเธญเธตเธขเธ/เธเธทเนเธเธชเธเธฒเธก
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// เธฃเธฒเธเธฒเธเนเธฒเนเธเนเธฒเธเธญเธฃเนเธเธเนเธญเธเธฑเนเธงเนเธกเธ (เธเธฒเธ)
    /// </summary>
    public decimal PricePerHour { get; set; }

    /// <summary>
    /// เธชเธเธฒเธเธฐเธเธญเธฃเนเธ
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// เธงเธฑเธเธเธตเนเธชเธฃเนเธฒเธ
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// เธงเธฑเธเธเธตเนเนเธเนเนเธเธฅเนเธฒเธชเธธเธ
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<BookingSlot> BookingSlots { get; set; } = new List<BookingSlot>();

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual ICollection<CourtBlock> CourtBlocks { get; set; } = new List<CourtBlock>();

    public virtual ICollection<CourtIssue> CourtIssues { get; set; } = new List<CourtIssue>();

    public virtual ICollection<WalkInQueue> WalkInQueues { get; set; } = new List<WalkInQueue>();
}
