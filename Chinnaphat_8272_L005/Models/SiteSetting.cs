using System;
using System.Collections.Generic;

namespace Chinnaphat_8272_L005.Models;

/// <summary>
/// เธเนเธฒเธเธฑเนเธเธฃเธฐเธเธ (QR เธฃเธฑเธเนเธเธดเธ, เธเธเธฃเธฐเนเธเธตเธขเธเธซเธเนเธฒ Home, เธเนเธขเธเธฒเธขเธขเธเนเธฅเธดเธ)
/// </summary>
public partial class SiteSetting
{
    /// <summary>
    /// PK เธเธทเนเธญเธเนเธฒเธเธฑเนเธ เนเธเนเธ payment_qr_url, house_rules, cancel_free_hours
    /// </summary>
    public string SettingKey { get; set; } = null!;

    /// <summary>
    /// เธเนเธฒเธเธตเนเธเธฑเนเธ
    /// </summary>
    public string SettingValue { get; set; } = null!;

    /// <summary>
    /// เธเธณเธญเธเธดเธเธฒเธข
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// FK -&gt; users (Admin)
    /// </summary>
    public uint? UpdatedBy { get; set; }

    /// <summary>
    /// เนเธเนเนเธเธฅเนเธฒเธชเธธเธ
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    public virtual User? UpdatedByNavigation { get; set; }
}
