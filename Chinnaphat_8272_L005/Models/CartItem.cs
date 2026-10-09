using System;
using System.Collections.Generic;

namespace Chinnaphat_8272_L005.Models;

/// <summary>
/// เธฃเธฒเธขเธเธฒเธฃเนเธเธเธฐเธเธฃเนเธฒ
/// </summary>
public partial class CartItem
{
    /// <summary>
    /// PK
    /// </summary>
    public uint CartItemId { get; set; }

    /// <summary>
    /// FK -&gt; carts.cart_id
    /// </summary>
    public uint CartId { get; set; }

    /// <summary>
    /// เธเธเธดเธเธฃเธฒเธขเธเธฒเธฃ: เธเธญเธฃเนเธ / เธเธทเนเธญ / เนเธเนเธฒ
    /// </summary>
    public string ItemType { get; set; } = null!;

    /// <summary>
    /// FK -&gt; courts (เนเธเนเนเธกเธทเนเธญ item_type=COURT_SLOT)
    /// </summary>
    public ushort? CourtId { get; set; }

    /// <summary>
    /// FK -&gt; time_slots (เนเธเนเนเธกเธทเนเธญ item_type=COURT_SLOT)
    /// </summary>
    public byte? SlotId { get; set; }

    /// <summary>
    /// เธงเธฑเธเธเธตเนเนเธฅเนเธ (เนเธเนเนเธกเธทเนเธญ item_type=COURT_SLOT)
    /// </summary>
    public DateOnly? PlayDate { get; set; }

    /// <summary>
    /// FK -&gt; products (เนเธเนเนเธกเธทเนเธญ item_type=SALE/RENT)
    /// </summary>
    public uint? ProductId { get; set; }

    /// <summary>
    /// เธเธณเธเธงเธ
    /// </summary>
    public uint Quantity { get; set; }

    /// <summary>
    /// เนเธงเธฅเธฒเธเธตเนเนเธชเนเธเธฐเธเธฃเนเธฒ
    /// </summary>
    public DateTime AddedAt { get; set; }

    public virtual Cart Cart { get; set; } = null!;

    public virtual Court? Court { get; set; }

    public virtual Product? Product { get; set; }

    public virtual TimeSlot? Slot { get; set; }
}
