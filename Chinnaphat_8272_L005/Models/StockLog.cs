using System;
using System.Collections.Generic;

namespace Chinnaphat_8272_L005.Models;

/// <summary>
/// เธเธฃเธฐเธงเธฑเธเธดเธเธฒเธฃเนเธเธฅเธทเนเธญเธเนเธซเธงเธชเธเนเธญเธ
/// </summary>
public partial class StockLog
{
    /// <summary>
    /// PK
    /// </summary>
    public ulong LogId { get; set; }

    /// <summary>
    /// FK -&gt; products.product_id
    /// </summary>
    public uint ProductId { get; set; }

    /// <summary>
    /// เธเธณเธเธงเธเธเธตเนเนเธเธฅเธตเนเธขเธ (+เนเธเธดเนเธก / -เธฅเธ)
    /// </summary>
    public int ChangeQty { get; set; }

    /// <summary>
    /// เธชเธฒเนเธซเธเธธเธเธฒเธฃเนเธเธฅเธตเนเธขเธเธชเธเนเธญเธ
    /// </summary>
    public string Reason { get; set; } = null!;

    /// <summary>
    /// เธญเนเธฒเธเธญเธดเธเธเธดเธฅ (เธเนเธฒเธกเธต) -&gt; bookings.booking_id
    /// </summary>
    public uint? BookingId { get; set; }

    /// <summary>
    /// เธซเธกเธฒเธขเนเธซเธเธธ
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// FK -&gt; users.user_id เธเธนเนเธเธณเธฃเธฒเธขเธเธฒเธฃ
    /// </summary>
    public uint CreatedBy { get; set; }

    /// <summary>
    /// เนเธงเธฅเธฒเธเธตเนเธเธฑเธเธเธถเธ
    /// </summary>
    public DateTime CreatedAt { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
