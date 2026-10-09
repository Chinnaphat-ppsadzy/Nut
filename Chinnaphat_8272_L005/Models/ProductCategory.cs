using System;
using System.Collections.Generic;

namespace Chinnaphat_8272_L005.Models;

/// <summary>
/// เธซเธกเธงเธเธซเธกเธนเนเธชเธดเธเธเนเธฒ
/// </summary>
public partial class ProductCategory
{
    /// <summary>
    /// PK
    /// </summary>
    public ushort CategoryId { get; set; }

    /// <summary>
    /// เธฃเธซเธฑเธชเธซเธกเธงเธ: SHUTTLECOCK, RACKET, SHOES, BEVERAGE, GRIP (เนเธเนเธญเนเธฒเธเธญเธดเธเนเธเนเธเธฃเนเธกเธเธฑเนเธ)
    /// </summary>
    public string CategoryCode { get; set; } = null!;

    /// <summary>
    /// เธเธทเนเธญเธซเธกเธงเธ
    /// </summary>
    public string CategoryName { get; set; } = null!;

    /// <summary>
    /// SALE=เธชเธดเธเธเนเธฒเธเธฒเธข, RENT=เธชเธดเธเธเนเธฒเนเธเนเธฒ
    /// </summary>
    public string ProductType { get; set; } = null!;

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual ICollection<PromotionRule> PromotionRules { get; set; } = new List<PromotionRule>();
}
