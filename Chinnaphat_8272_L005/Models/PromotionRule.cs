using System;
using System.Collections.Generic;

namespace Chinnaphat_8272_L005.Models;

/// <summary>
/// เนเธเธทเนเธญเธเนเธเนเธฅเธฐเธฃเธฒเธเธงเธฑเธฅเธเธญเธเนเธเนเธฅเธฐเนเธเธฃเนเธกเธเธฑเนเธ
/// </summary>
public partial class PromotionRule
{
    /// <summary>
    /// PK
    /// </summary>
    public uint RuleId { get; set; }

    /// <summary>
    /// FK -&gt; promotions.promotion_id
    /// </summary>
    public uint PromotionId { get; set; }

    /// <summary>
    /// CONDITION=เนเธเธทเนเธญเธเนเธเธเธตเนเธเนเธญเธเธกเธต, REWARD=เธชเธดเนเธเธเธตเนเนเธเนเธฃเธฑเธ/เธเธนเธเธฅเธเธฃเธฒเธเธฒ
    /// </summary>
    public string RuleKind { get; set; } = null!;

    /// <summary>
    /// เนเธเนเธฒเธซเธกเธฒเธข: เธเนเธฒเธเธญเธฃเนเธ / เธชเธดเธเธเนเธฒเธเธทเนเธญ / เธชเธดเธเธเนเธฒเนเธเนเธฒ / เธขเธญเธเธฃเธงเธกเธเธดเธฅ
    /// </summary>
    public string TargetType { get; set; } = null!;

    /// <summary>
    /// FK -&gt; product_categories (เธซเธกเธงเธเธเธตเนเนเธเธตเนเธขเธงเธเนเธญเธ)
    /// </summary>
    public ushort? CategoryId { get; set; }

    /// <summary>
    /// เธเธณเธเธงเธเธเธฑเนเธเธเนเธณเธเธตเนเธเนเธญเธเธกเธต/เธเธณเธเธงเธเธเธตเนเนเธเนเธฃเธฑเธ
    /// </summary>
    public uint RequiredQty { get; set; }

    /// <summary>
    /// เธฅเธเนเธเธเธฒเธฐเธเธดเนเธเธเธตเน N (เนเธเนเธ 2 = เธซเธฅเธญเธเธเธตเน 2)
    /// </summary>
    public uint? NthItem { get; set; }

    public virtual ProductCategory? Category { get; set; }

    public virtual Promotion Promotion { get; set; } = null!;
}
