using System;
using System.Collections.Generic;

namespace Chinnaphat_8272_L005.Models;

/// <summary>
/// เธเธฒเธฃเธฅเนเธญเธเธเธญเธฃเนเธเธเธฑเนเธงเธเธฃเธฒเธง
/// </summary>
public partial class CourtBlock
{
    /// <summary>
    /// PK
    /// </summary>
    public uint BlockId { get; set; }

    /// <summary>
    /// FK -&gt; courts.court_id
    /// </summary>
    public ushort CourtId { get; set; }

    /// <summary>
    /// เนเธฃเธดเนเธกเธฅเนเธญเธ
    /// </summary>
    public DateTime BlockStart { get; set; }

    /// <summary>
    /// เธชเธดเนเธเธชเธธเธเธเธฒเธฃเธฅเนเธญเธ
    /// </summary>
    public DateTime BlockEnd { get; set; }

    /// <summary>
    /// เนเธซเธเธธเธเธฅ เนเธเนเธ เธชเธเธฒเธกเธเธฑเธ/เธเนเธญเธกเธเธณเธฃเธธเธ
    /// </summary>
    public string Reason { get; set; } = null!;

    /// <summary>
    /// FK -&gt; users.user_id (เธเนเธฒเธขเธเธนเนเธฅเธเธญเธฃเนเธเธเธตเนเธฅเนเธญเธ)
    /// </summary>
    public uint CreatedBy { get; set; }

    /// <summary>
    /// เธงเธฑเธเธเธตเนเธเธฑเธเธเธถเธ
    /// </summary>
    public DateTime CreatedAt { get; set; }

    public virtual Court Court { get; set; } = null!;

    public virtual User CreatedByNavigation { get; set; } = null!;
}
