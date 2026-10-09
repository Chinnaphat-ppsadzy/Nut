using System;
using System.Collections.Generic;

namespace Chinnaphat_8272_L005.Models;

/// <summary>
/// เธเธฒเธฃเนเธเนเธเธญเธธเธเธเธฃเธเน/เธชเธเธฒเธกเธเธณเธฃเธธเธเธเธถเธ Admin
/// </summary>
public partial class CourtIssue
{
    /// <summary>
    /// PK
    /// </summary>
    public uint IssueId { get; set; }

    /// <summary>
    /// FK -&gt; courts.court_id (เธเนเธฒเนเธเธตเนเธขเธงเธเธฑเธเธเธญเธฃเนเธเนเธเธเธญเธฃเนเธเธซเธเธถเนเธ)
    /// </summary>
    public ushort? CourtId { get; set; }

    /// <summary>
    /// FK -&gt; users.user_id เธเธนเนเนเธเนเธ
    /// </summary>
    public uint ReportedBy { get; set; }

    /// <summary>
    /// เธฃเธฒเธขเธฅเธฐเนเธญเธตเธขเธเธเธฑเธเธซเธฒ/เธญเธธเธเธเธฃเธเนเธเธณเธฃเธธเธ
    /// </summary>
    public string Description { get; set; } = null!;

    /// <summary>
    /// เธชเธเธฒเธเธฐเธเธฒเธฃเนเธเนเนเธ
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// FK -&gt; users.user_id (Admin เธเธนเนเธเธดเธเนเธฃเธทเนเธญเธ)
    /// </summary>
    public uint? ResolvedBy { get; set; }

    /// <summary>
    /// เธงเธฑเธเธเธตเนเนเธเนเนเธเนเธชเธฃเนเธ
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// เธงเธฑเธเธเธตเนเนเธเนเธ
    /// </summary>
    public DateTime CreatedAt { get; set; }

    public virtual Court? Court { get; set; }

    public virtual User ReportedByNavigation { get; set; } = null!;

    public virtual User? ResolvedByNavigation { get; set; }
}
