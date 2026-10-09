using System;
using System.Collections.Generic;

namespace Chinnaphat_8272_L005.Models;

/// <summary>
/// เนเธเนเธชเธฃเนเธเธฃเธฑเธเนเธเธดเธ / เนเธเธเธณเธเธฑเธเธ�เธฒเธฉเธต
/// </summary>
public partial class Receipt
{
    /// <summary>
    /// PK
    /// </summary>
    public uint ReceiptId { get; set; }

    /// <summary>
    /// เนเธฅเธเธเธตเนเนเธเนเธชเธฃเนเธ/เนเธเธเธณเธเธฑเธเธ�เธฒเธฉเธต
    /// </summary>
    public string ReceiptNo { get; set; } = null!;

    /// <summary>
    /// FK -&gt; bookings.booking_id
    /// </summary>
    public uint BookingId { get; set; }

    /// <summary>
    /// FK -&gt; payments.payment_id
    /// </summary>
    public uint PaymentId { get; set; }

    /// <summary>
    /// เนเธเนเธชเธฃเนเธเธฃเธฑเธเนเธเธดเธ / เนเธเธเธณเธเธฑเธเธ�เธฒเธฉเธต
    /// </summary>
    public string DocType { get; set; } = null!;

    /// <summary>
    /// เธเธทเนเธญเธเธตเนเธญเธญเธเนเธญเธเธชเธฒเธฃ
    /// </summary>
    public string CustomerName { get; set; } = null!;

    /// <summary>
    /// เนเธฅเธเธเธฃเธฐเธเธณเธเธฑเธงเธเธนเนเนเธชเธตเธขเธ�เธฒเธฉเธต (เธเนเธฒเธเธญเนเธเธเธณเธเธฑเธเธ�เธฒเธฉเธต)
    /// </summary>
    public string? TaxId { get; set; }

    /// <summary>
    /// เธเธตเนเธญเธขเธนเนเธญเธญเธเนเธญเธเธชเธฒเธฃ
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// เธขเธญเธเธฃเธงเธก
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// เธ�เธฒเธฉเธตเธกเธนเธฅเธเนเธฒเนเธเธดเนเธก
    /// </summary>
    public decimal VatAmount { get; set; }

    /// <summary>
    /// FK -&gt; users (เธเนเธฒเธขเธเธฑเธเธเธต)
    /// </summary>
    public uint IssuedBy { get; set; }

    /// <summary>
    /// เธงเธฑเธเธเธตเนเธญเธญเธ
    /// </summary>
    public DateTime IssuedAt { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual User IssuedByNavigation { get; set; } = null!;

    public virtual Payment Payment { get; set; } = null!;
}
