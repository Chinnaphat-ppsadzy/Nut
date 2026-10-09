using System;
using System.Collections.Generic;

namespace Chinnaphat_8272_L005.Models;

/// <summary>
/// เธเธงเธฒเธกเนเธชเธตเธขเธซเธฒเธขเธเธญเธเธญเธธเธเธเธฃเธเนเนเธเนเธฒเนเธฅเธฐเธเนเธฒเธเธฃเธฑเธ
/// </summary>
public partial class EquipmentDamage
{
    /// <summary>
    /// PK
    /// </summary>
    public uint DamageId { get; set; }

    /// <summary>
    /// FK -&gt; booking_items (เธญเธธเธเธเธฃเธเนเนเธเนเธฒเธเธตเนเนเธชเธตเธขเธซเธฒเธข)
    /// </summary>
    public uint BookingItemId { get; set; }

    /// <summary>
    /// FK -&gt; users (เธเนเธฒเธขเธเธทเนเธญ-เนเธเนเธฒเธเธนเนเธเธฃเธงเธเธเธ)
    /// </summary>
    public uint ReportedBy { get; set; }

    /// <summary>
    /// เธฃเธฒเธขเธฅเธฐเนเธญเธตเธขเธเธเธงเธฒเธกเนเธชเธตเธขเธซเธฒเธข เนเธเนเธ เนเธกเนเนเธเธเธซเธฑเธ
    /// </summary>
    public string Description { get; set; } = null!;

    /// <summary>
    /// เธฃเธนเธเธซเธฅเธฑเธเธเธฒเธ
    /// </summary>
    public string? PhotoUrl { get; set; }

    /// <summary>
    /// เธเนเธฒเธเธฃเธฑเธเธเธตเนเธเนเธฒเธขเธเธฑเธเธเธตเธเธณเธซเธเธ
    /// </summary>
    public decimal FineAmount { get; set; }

    /// <summary>
    /// เนเธเนเธเนเธฅเนเธง/เธเธดเธเธเนเธฒเธเธฃเธฑเธเนเธฅเนเธง/เธเธณเธฃเธฐเนเธฅเนเธง/เธขเธเนเธงเนเธ
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// FK -&gt; users (เธเนเธฒเธขเธเธฑเธเธเธต)
    /// </summary>
    public uint? ProcessedBy { get; set; }

    /// <summary>
    /// เธงเธฑเธเธเธตเนเธเธดเธเธเนเธฒเธเธฃเธฑเธ
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// เธงเธฑเธเธเธตเนเนเธเนเธ
    /// </summary>
    public DateTime CreatedAt { get; set; }

    public virtual BookingItem BookingItem { get; set; } = null!;

    public virtual User? ProcessedByNavigation { get; set; }

    public virtual User ReportedByNavigation { get; set; } = null!;
}
