using System;
using System.Collections.Generic;

namespace Chinnaphat_8272_L005.Models;

/// <summary>
/// เธเธเธเธฒเธเธเธนเนเนเธเนเธเธฒเธ (5 เธเธเธเธฒเธ)
/// </summary>
public partial class Role
{
    /// <summary>
    /// PK เธฃเธซเธฑเธชเธเธเธเธฒเธ
    /// </summary>
    public byte RoleId { get; set; }

    /// <summary>
    /// เธฃเธซเธฑเธชเธเธเธเธฒเธ: CUSTOMER, COURT_STAFF, SALES_STAFF, ACCOUNTING, ADMIN
    /// </summary>
    public string RoleCode { get; set; } = null!;

    /// <summary>
    /// เธเธทเนเธญเธเธเธเธฒเธ (เนเธชเธเธเธเธฅ)
    /// </summary>
    public string RoleName { get; set; } = null!;

    /// <summary>
    /// เธเธณเธญเธเธดเธเธฒเธขเธเธเธเธฒเธ
    /// </summary>
    public string? Description { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();

    public virtual ICollection<Permission> Permissions { get; set; } = new List<Permission>();
}
