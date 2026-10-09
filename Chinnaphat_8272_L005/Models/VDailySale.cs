using System;
using System.Collections.Generic;

namespace Chinnaphat_8272_L005.Models;

public partial class VDailySale
{
    public DateOnly? SalesDate { get; set; }

    public decimal? CourtRevenue { get; set; }

    public decimal? ProductRevenue { get; set; }

    public decimal? RentalRevenue { get; set; }

    public decimal? TotalDiscount { get; set; }

    public decimal? NetRevenue { get; set; }

    public long BookingsCount { get; set; }
}
