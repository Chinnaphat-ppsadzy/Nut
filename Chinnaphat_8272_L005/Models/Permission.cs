using System;
using System.Collections.Generic;

namespace Chinnaphat_8272_L005.Models;

/// <summary>
/// เธฃเธฒเธขเธเธฒเธฃเธชเธดเธเธเธดเนเธเธฒเธฃเนเธเนเธเธฒเธเธขเนเธญเธข
/// </summary>
public partial class Permission
{
    /// <summary>
    /// PK เธฃเธซเธฑเธชเธชเธดเธเธเธดเน
    /// </summary>
    public ushort PermissionId { get; set; }

    /// <summary>
    /// เธฃเธซเธฑเธชเธชเธดเธเธเธดเน เนเธเนเธ PAYMENT_VERIFY
    /// </summary>
    public string PermissionCode { get; set; } = null!;

    /// <summary>
    /// เธเธทเนเธญเธชเธดเธเธเธดเน (เนเธชเธเธเธเธฅ)
    /// </summary>
    public string PermissionName { get; set; } = null!;

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
