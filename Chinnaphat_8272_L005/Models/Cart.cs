using System;
using System.Collections.Generic;

namespace Chinnaphat_8272_L005.Models;

/// <summary>
/// เธเธฐเธเธฃเนเธฒเธชเธดเธเธเนเธฒ/เธเธญเธฃเนเธเธเธญเธเธฅเธนเธเธเนเธฒ
/// </summary>
public partial class Cart
{
    /// <summary>
    /// PK
    /// </summary>
    public uint CartId { get; set; }

    /// <summary>
    /// FK -&gt; users.user_id เนเธเนเธฒเธเธญเธเธเธฐเธเธฃเนเธฒ
    /// </summary>
    public uint UserId { get; set; }

    /// <summary>
    /// เธชเธเธฒเธเธฐเธเธฐเธเธฃเนเธฒ
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// เธงเธฑเธเธเธตเนเธชเธฃเนเธฒเธ
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// เนเธเนเนเธเธฅเนเธฒเธชเธธเธ
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual User User { get; set; } = null!;
}
