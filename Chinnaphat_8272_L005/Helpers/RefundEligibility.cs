using Chinnaphat_8272_L005.Models;

namespace Chinnaphat_8272_L005.Helpers;

/// <summary>Only purchased badminton gear is eligible for product refunds.</summary>
public static class RefundEligibility
{
    public static bool IsRefundableEquipment(BookingItem? item)
    {
        if (item is null || item.ItemType != "SALE" || item.LineTotal <= 0)
            return false;

        var categoryCode = item.Product?.Category?.CategoryCode;
        return string.Equals(categoryCode, "SHUTTLECOCK", StringComparison.OrdinalIgnoreCase)
            || string.Equals(categoryCode, "GRIP", StringComparison.OrdinalIgnoreCase);
    }
}
