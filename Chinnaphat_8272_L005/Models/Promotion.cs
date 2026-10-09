using System;
using System.Collections.Generic;

namespace Chinnaphat_8272_L005.Models;

/// <summary>
/// เนเธเธกเนเธเธเนเธเธฃเนเธกเธเธฑเนเธ (Admin เนเธเธดเธ-เธเธดเธ/เนเธเนเนเธเนเธเน)
/// </summary>
public partial class Promotion
{
    /// <summary>
    /// PK
    /// </summary>
    public uint PromotionId { get; set; }

    /// <summary>
    /// เธฃเธซเธฑเธชเนเธเธฃเนเธกเธเธฑเนเธ
    /// </summary>
    public string PromoCode { get; set; } = null!;

    /// <summary>
    /// เธเธทเนเธญเนเธเธฃเนเธกเธเธฑเนเธ
    /// </summary>
    public string PromoName { get; set; } = null!;

    /// <summary>
    /// เธฃเธฒเธขเธฅเธฐเนเธญเธตเธขเธเธเธตเนเนเธชเธเธเธซเธเนเธฒ Home/Checkout
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// เธฃเธนเธเนเธเธเนเธเธฃเธฏ 5 เนเธเธ
    /// </summary>
    public string PromoType { get; set; } = null!;

    /// <summary>
    /// เธเธเธดเธเธชเนเธงเธเธฅเธ: เนเธเธญเธฃเนเนเธเนเธเธเน / เธเธณเธเธงเธเนเธเธดเธ / เธเธญเธเนเธเธกเธเธฃเธต
    /// </summary>
    public string DiscountType { get; set; } = null!;

    /// <summary>
    /// เธเนเธฒเธชเนเธงเธเธฅเธ (เนเธเนเธ 20 = 20% เธซเธฃเธทเธญ 100 เธเธฒเธ / เธเธณเธเธงเธเธเธดเนเธเธเธตเนเธเธฃเธต)
    /// </summary>
    public decimal DiscountValue { get; set; }

    /// <summary>
    /// เธขเธญเธเธเธดเธฅเธเธฑเนเธเธเนเธณ (เนเธเธฃเธฏ 5)
    /// </summary>
    public decimal? MinBillAmount { get; set; }

    /// <summary>
    /// 1=เนเธเธเธฒเธฐเธชเธกเธฒเธเธดเธเนเธซเธกเนเธเธตเนเธเธญเธเธเธฃเธฑเนเธเนเธฃเธ
    /// </summary>
    public bool FirstBookingOnly { get; set; }

    /// <summary>
    /// เธเนเธงเธเนเธงเธฅเธฒเนเธฃเธดเนเธกเธเธญเธเนเธเธฃเธฏ (เนเธเธฃเธฏ 1)
    /// </summary>
    public TimeOnly? TimeStart { get; set; }

    /// <summary>
    /// เธเนเธงเธเนเธงเธฅเธฒเธชเธดเนเธเธชเธธเธเธเธญเธเนเธเธฃเธฏ (เนเธเธฃเธฏ 1)
    /// </summary>
    public TimeOnly? TimeEnd { get; set; }

    /// <summary>
    /// เธงเธฑเธเนเธฃเธดเนเธกเนเธเธกเนเธเธ
    /// </summary>
    public DateOnly? StartDate { get; set; }

    /// <summary>
    /// เธงเธฑเธเธชเธดเนเธเธชเธธเธเนเธเธกเนเธเธ
    /// </summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>
    /// 1=เนเธเนเธฃเนเธงเธกเธเธฑเธเนเธเธฃเธฏ เธญเธทเนเธเนเธเน
    /// </summary>
    public bool? IsStackable { get; set; }

    /// <summary>
    /// เธฅเธณเธเธฑเธเธเธฒเธฃเธเธณเธเธงเธ (เธเนเธญเธขเธเนเธญเธ)
    /// </summary>
    public short Priority { get; set; }

    /// <summary>
    /// 1=เนเธเธดเธเนเธเนเธเธฒเธ
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// FK -&gt; users (Admin)
    /// </summary>
    public uint? CreatedBy { get; set; }

    /// <summary>
    /// เธงเธฑเธเธเธตเนเธชเธฃเนเธฒเธ
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// เนเธเนเนเธเธฅเนเธฒเธชเธธเธ
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<BookingPromotion> BookingPromotions { get; set; } = new List<BookingPromotion>();

    public virtual User? CreatedByNavigation { get; set; }

    public virtual ICollection<PromotionRule> PromotionRules { get; set; } = new List<PromotionRule>();
}
