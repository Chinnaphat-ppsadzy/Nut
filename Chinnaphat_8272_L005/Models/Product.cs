using System;
using System.Collections.Generic;

namespace Chinnaphat_8272_L005.Models;

/// <summary>
/// เธชเธดเธเธเนเธฒเธเธฒเธข เนเธฅเธฐ เธชเธดเธเธเนเธฒเนเธเนเธฒ
/// </summary>
public partial class Product
{
    /// <summary>
    /// PK
    /// </summary>
    public uint ProductId { get; set; }

    /// <summary>
    /// FK -&gt; product_categories.category_id
    /// </summary>
    public ushort CategoryId { get; set; }

    /// <summary>
    /// เธฃเธซเธฑเธชเธชเธดเธเธเนเธฒ
    /// </summary>
    public string Sku { get; set; } = null!;

    /// <summary>
    /// เธเธทเนเธญเธชเธดเธเธเนเธฒ/เธฃเธธเนเธ
    /// </summary>
    public string ProductName { get; set; } = null!;

    /// <summary>
    /// เธเธฃเธฐเนเธ�เธ: เธเธฒเธข/เนเธเนเธฒ (เธเนเธญเธเธเธฃเธเธเธฑเธเธซเธกเธงเธ)
    /// </summary>
    public string ProductType { get; set; } = null!;

    /// <summary>
    /// เธฃเธฒเธเธฒเธเธฒเธข เธซเธฃเธทเธญ เธฃเธฒเธเธฒเนเธเนเธฒเธเนเธญเธเธฃเธฑเนเธ (เธเธฒเธ)
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// เธเนเธฒเธกเธฑเธเธเธณ (เนเธเธเธฒเธฐเธชเธดเธเธเนเธฒเนเธเนเธฒ, เธเนเธฒเธกเธต)
    /// </summary>
    public decimal Deposit { get; set; }

    /// <summary>
    /// เธซเธเนเธงเธข เนเธเนเธ เธซเธฅเธญเธ, เธเธงเธ, เนเธกเน, เธเธนเน
    /// </summary>
    public string Unit { get; set; } = null!;

    /// <summary>
    /// เธเธณเธเธงเธเธเธเนเธซเธฅเธทเธญ (เธเธฒเธข) / เธเธณเธเธงเธเธเธตเนเธกเธตเนเธซเนเนเธเนเธฒเธเธฑเนเธเธซเธกเธ (เนเธเนเธฒ)
    /// </summary>
    public int StockQty { get; set; }

    /// <summary>
    /// เธเธตเนเธญเธขเธนเนเธฃเธนเธเธชเธดเธเธเนเธฒ
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// 1=เนเธชเธเธเธเธฒเธข/เนเธเนเธฒ
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// เธงเธฑเธเธเธตเนเธชเธฃเนเธฒเธ
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// เธงเธฑเธเธเธตเนเนเธเนเนเธเธฅเนเธฒเธชเธธเธ
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<BookingItem> BookingItems { get; set; } = new List<BookingItem>();

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual ProductCategory Category { get; set; } = null!;

    public virtual ICollection<StockLog> StockLogs { get; set; } = new List<StockLog>();
}
