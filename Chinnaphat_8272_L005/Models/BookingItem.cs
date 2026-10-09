using System;
using System.Collections.Generic;

namespace Chinnaphat_8272_L005.Models;

/// <summary>
/// เธฃเธฒเธขเธเธฒเธฃเธชเธดเธเธเนเธฒเธเธทเนเธญ/เนเธเนเธฒเนเธเธเธดเธฅ
/// </summary>
public partial class BookingItem
{
    /// <summary>
    /// PK
    /// </summary>
    public uint BookingItemId { get; set; }

    /// <summary>
    /// FK -&gt; bookings.booking_id
    /// </summary>
    public uint BookingId { get; set; }

    /// <summary>
    /// FK -&gt; products.product_id
    /// </summary>
    public uint ProductId { get; set; }

    /// <summary>
    /// เธเธทเนเธญ/เนเธเนเธฒ
    /// </summary>
    public string ItemType { get; set; } = null!;

    /// <summary>
    /// เธเธณเธเธงเธ
    /// </summary>
    public uint Quantity { get; set; }

    /// <summary>
    /// เธฃเธฒเธเธฒเธเนเธญเธซเธเนเธงเธข เธ เธเธญเธเธชเธฑเนเธ (snapshot)
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// เธชเนเธงเธเธฅเธเธเธญเธเธฃเธฒเธขเธเธฒเธฃเธเธตเน (เธฃเธงเธกเธเธฃเธเธตเธเธญเธเธเธฃเธต)
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// เธขเธญเธเธฃเธฒเธขเธเธฒเธฃเธซเธฅเธฑเธเธซเธฑเธเธชเนเธงเธเธฅเธ
    /// </summary>
    public decimal LineTotal { get; set; }

    /// <summary>
    /// เธเธญเธ/เธชเนเธเธกเธญเธเนเธฅเนเธง/เธเธทเธเนเธฅเนเธง/เนเธชเธตเธขเธซเธฒเธข/เธชเธนเธเธซเธฒเธข/เธขเธเนเธฅเธดเธ
    /// </summary>
    public string ItemStatus { get; set; } = null!;

    /// <summary>
    /// เนเธงเธฅเธฒเธชเนเธเธกเธญเธ
    /// </summary>
    public DateTime? HandedOverAt { get; set; }

    /// <summary>
    /// FK -&gt; users (เธเนเธฒเธขเธเธทเนเธญ-เนเธเนเธฒ)
    /// </summary>
    public uint? HandedOverBy { get; set; }

    /// <summary>
    /// เนเธงเธฅเธฒเธฃเธฑเธเธเธทเธ (เนเธเธเธฒเธฐเนเธเนเธฒ)
    /// </summary>
    public DateTime? ReturnedAt { get; set; }

    /// <summary>
    /// FK -&gt; users (เธเนเธฒเธขเธเธทเนเธญ-เนเธเนเธฒ)
    /// </summary>
    public uint? ReturnedBy { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual ICollection<EquipmentDamage> EquipmentDamages { get; set; } = new List<EquipmentDamage>();

    public virtual User? HandedOverByNavigation { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual User? ReturnedByNavigation { get; set; }
}
