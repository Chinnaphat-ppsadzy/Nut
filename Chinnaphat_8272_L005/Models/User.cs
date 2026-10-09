using System;
using System.Collections.Generic;

namespace Chinnaphat_8272_L005.Models;

/// <summary>
/// เธเธนเนเนเธเนเธเธฒเธเธเธธเธเธเธเธเธฒเธ
/// </summary>
public partial class User
{
    /// <summary>
    /// PK เธฃเธซเธฑเธชเธเธนเนเนเธเน
    /// </summary>
    public uint UserId { get; set; }

    /// <summary>
    /// FK -&gt; roles.role_id
    /// </summary>
    public byte RoleId { get; set; }

    /// <summary>
    /// เธญเธตเนเธกเธฅเธชเธณเธซเธฃเธฑเธ Login (เนเธกเนเธเนเธณ)
    /// </summary>
    public string Email { get; set; } = null!;

    /// <summary>
    /// เธฃเธซเธฑเธชเธเนเธฒเธเธเธตเนเนเธเนเธฒเธฃเธซเธฑเธชเนเธฅเนเธง (bcrypt/argon2)
    /// </summary>
    public string PasswordHash { get; set; } = null!;

    /// <summary>
    /// เธเธทเนเธญ-เธเธฒเธกเธชเธเธธเธฅ
    /// </summary>
    public string FullName { get; set; } = null!;

    /// <summary>
    /// เนเธเธญเธฃเนเนเธเธฃเธจเธฑเธเธเน
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// เนเธเธขเธเธญเธเธชเธณเนเธฃเนเธเนเธฅเนเธงเธซเธฃเธทเธญเธขเธฑเธ (0=เธชเธกเธฒเธเธดเธเนเธซเธกเน เนเธเนเนเธเนเธเนเธเธฃเธฏ เธขเธญเธเธฃเธงเธก)
    /// </summary>
    public bool HasBooked { get; set; }

    /// <summary>
    /// เธชเธเธฒเธเธฐเธเธฑเธเธเธต
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// เนเธงเธฅเธฒเนเธเนเธฒเธชเธนเนเธฃเธฐเธเธเธฅเนเธฒเธชเธธเธ
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// เธงเธฑเธเธเธตเนเธชเธกเธฑเธเธฃ
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// เธงเธฑเธเธเธตเนเนเธเนเนเธเธฅเนเธฒเธชเธธเธ
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<Booking> BookingCheckedInByNavigations { get; set; } = new List<Booking>();

    public virtual ICollection<Booking> BookingCheckedOutByNavigations { get; set; } = new List<Booking>();

    public virtual ICollection<BookingItem> BookingItemHandedOverByNavigations { get; set; } = new List<BookingItem>();

    public virtual ICollection<BookingItem> BookingItemReturnedByNavigations { get; set; } = new List<BookingItem>();

    public virtual ICollection<Booking> BookingUsers { get; set; } = new List<Booking>();

    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();

    public virtual ICollection<CourtBlock> CourtBlocks { get; set; } = new List<CourtBlock>();

    public virtual ICollection<CourtIssue> CourtIssueReportedByNavigations { get; set; } = new List<CourtIssue>();

    public virtual ICollection<CourtIssue> CourtIssueResolvedByNavigations { get; set; } = new List<CourtIssue>();

    public virtual ICollection<EquipmentDamage> EquipmentDamageProcessedByNavigations { get; set; } = new List<EquipmentDamage>();

    public virtual ICollection<EquipmentDamage> EquipmentDamageReportedByNavigations { get; set; } = new List<EquipmentDamage>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Promotion> Promotions { get; set; } = new List<Promotion>();

    public virtual ICollection<Receipt> Receipts { get; set; } = new List<Receipt>();

    public virtual ICollection<Refund> Refunds { get; set; } = new List<Refund>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<SiteSetting> SiteSettings { get; set; } = new List<SiteSetting>();

    public virtual ICollection<StockLog> StockLogs { get; set; } = new List<StockLog>();

    public virtual ICollection<WalkInQueue> WalkInQueues { get; set; } = new List<WalkInQueue>();
}
