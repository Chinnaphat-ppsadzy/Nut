using System;
using System.Collections.Generic;

namespace Chinnaphat_8272_L005.Models;

public partial class VPeakTime
{
    /// <summary>
    /// เนเธงเธฅเธฒเนเธฃเธดเนเธก
    /// </summary>
    public TimeOnly StartTime { get; set; }

    /// <summary>
    /// เนเธงเธฅเธฒเธชเธดเนเธเธชเธธเธ
    /// </summary>
    public TimeOnly EndTime { get; set; }

    public long TotalBooked { get; set; }
}
