using System;
using System.Collections.Generic;
using Chinnaphat_8272_L005.Models;

namespace Chinnaphat_8272_L005.ViewModels
{
    public class AdminDashboardViewModel
    {
        // 4 KPI Summary
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int NewUsersThisMonth { get; set; }

        public int TotalCourts { get; set; }
        public int AvailableCourts { get; set; }

        public int TotalBookings { get; set; }
        public int BookingsThisMonth { get; set; }
        public int PendingBookings { get; set; }

        public decimal TotalRevenue { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public decimal ApprovedBookingRevenue { get; set; }
        public decimal PaidWalkInRevenue { get; set; }
        public decimal PosSalesRevenue { get; set; }

        // Additional Metrics
        public int ActivePromotions { get; set; }
        public int TotalProducts { get; set; }
        public int LowStockProducts { get; set; }
        public int PendingIssues { get; set; }

        // Court Utilization
        public List<CourtUsageStat> CourtStats { get; set; } = new();

        // Recent Activity Feed
        public List<AdminActivityItem> RecentActivities { get; set; } = new();

        // Recent Users
        public List<User> RecentUsers { get; set; } = new();

        // Recent Bookings
        public List<Booking> RecentBookings { get; set; } = new();
    }

    public class CourtUsageStat
    {
        public ushort CourtId { get; set; }
        public string CourtName { get; set; } = string.Empty;
        public int TotalSlotsBooked { get; set; }
        public int Percentage { get; set; }
        public string Status { get; set; } = "AVAILABLE";
        public decimal TotalCourtRevenue { get; set; }
        public string Color { get; set; } = "#2d6a4f";
    }

    public class AdminActivityItem
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string TimeAgo { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Icon { get; set; } = "bi-circle";
        public string BadgeColor { get; set; } = "success";
    }
}
