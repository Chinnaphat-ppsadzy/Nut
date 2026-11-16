using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Chinnaphat_8272_L005.Models;

namespace Chinnaphat_8272_L005.Controllers
{
    public class CourtAmenityState
    {
        public bool LightsOn { get; set; } = false;
        public bool FansOn { get; set; } = false;
        public DateTime? LightsTurnedOnAt { get; set; }
        public DateTime? FansTurnedOnAt { get; set; }
    }

    public class CourtStaffController : Controller
    {
        private readonly Csi402dbContext _db;

        // In-memory thread-safe state for Court Amenities (Lights & Fans)
        private static readonly ConcurrentDictionary<ushort, CourtAmenityState> _courtAmenities = new();

        public CourtStaffController(Csi402dbContext db)
        {
            _db = db;
        }

        // ============================================================
        // 1. DAILY SCHEDULE & AMENITY CONTROL (ดูตารางการจองประจำวัน & ควบคุมไฟ/พัดลม)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Index(string? date)
        {
            ViewData["Title"] = "ตารางการจองประจำวัน & ควบคุมไฟ/พัดลม";
            await EnsureCourtStaffRoleAndUserAsync();

            string selectedDateStr = string.IsNullOrWhiteSpace(date)
                ? DateTime.Today.ToString("yyyy-MM-dd")
                : date.Trim();

            if (!DateOnly.TryParse(selectedDateStr, out DateOnly targetDate))
            {
                targetDate = DateOnly.FromDateTime(DateTime.Today);
                selectedDateStr = targetDate.ToString("yyyy-MM-dd");
            }

            ViewBag.SelectedDate = selectedDateStr;

            // ดึงข้อมูลสนาม
            var courts = await _db.Courts.OrderBy(c => c.CourtId).ToListAsync();
            if (!courts.Any())
            {
                courts = new List<Court>
                {
                    new() { CourtId = 1, CourtName = "Court 1", Description = "พื้นยาง BWF มาตรฐานสากล", PricePerHour = 200, Status = "AVAILABLE" },
                    new() { CourtId = 2, CourtName = "Court 2", Description = "พื้นยาง BWF มาตรฐานสากล", PricePerHour = 200, Status = "AVAILABLE" },
                    new() { CourtId = 3, CourtName = "Court 3", Description = "พื้นยาง BWF มาตรฐานสากล", PricePerHour = 200, Status = "AVAILABLE" },
                    new() { CourtId = 4, CourtName = "Court 4", Description = "พื้นยาง BWF มาตรฐานสากล", PricePerHour = 200, Status = "AVAILABLE" }
                };
                _db.Courts.AddRange(courts);
                await _db.SaveChangesAsync();
            }
            ViewBag.Courts = courts;

            // ดึง TimeSlots
            var timeSlots = await _db.TimeSlots
                .Where(s => s.IsActive == true || s.IsActive == null)
                .OrderBy(s => s.SlotId)
                .ToListAsync();

            if (!timeSlots.Any())
            {
                timeSlots = new List<TimeSlot>();
                for (int h = 8; h <= 21; h++)
                {
                    timeSlots.Add(new TimeSlot
                    {
                        SlotId = (byte)(h - 7),
                        StartTime = new TimeOnly(h, 0),
                        EndTime = new TimeOnly(h + 1, 0),
                        IsActive = true
                    });
                }
                _db.TimeSlots.AddRange(timeSlots);
                await _db.SaveChangesAsync();
            }
            ViewBag.TimeSlots = timeSlots;

            // ดึงการจองของวันที่เลือก
            var bookingsToday = await _db.Bookings
                .Include(b => b.User)
                .Include(b => b.BookingSlots).ThenInclude(bs => bs.Court)
                .Include(b => b.BookingSlots).ThenInclude(bs => bs.Slot)
                .Include(b => b.BookingItems).ThenInclude(bi => bi.Product)
                .Include(b => b.CheckedInByNavigation)
                .Include(b => b.CheckedOutByNavigation)
                .Where(b => b.PlayDate == targetDate)
                .OrderBy(b => b.CreatedAt)
                .ToListAsync();

            ViewBag.BookingsToday = bookingsToday;

            // ดึงรายการบล็อกคอร์ท (Court Blocks) ที่ครอบคลุมวันที่เลือก
            DateTime dayStart = targetDate.ToDateTime(TimeOnly.MinValue);
            DateTime dayEnd = targetDate.ToDateTime(TimeOnly.MaxValue);

            var activeBlocks = await _db.CourtBlocks
                .Include(cb => cb.Court)
                .Include(cb => cb.CreatedByNavigation)
                .Where(cb => cb.BlockStart <= dayEnd && cb.BlockEnd >= dayStart)
                .ToListAsync();

            ViewBag.ActiveBlocks = activeBlocks;

            // เตรียมสถานะอุปกรณ์ไฟ/พัดลมสำหรับแต่ละคอร์ท
            var amenityMap = new Dictionary<ushort, CourtAmenityState>();
            foreach (var court in courts)
            {
                var state = _courtAmenities.GetOrAdd(court.CourtId, _ => new CourtAmenityState());
                amenityMap[court.CourtId] = state;
            }
            ViewBag.Amenities = amenityMap;

            // สถิติด่วนประจำวัน
            ViewBag.TotalBookingsCount = bookingsToday.Count;
            ViewBag.CheckedInCount = bookingsToday.Count(b => b.Status == "CHECKED_IN" || b.Status == "IN_USE");
            ViewBag.CompletedCount = bookingsToday.Count(b => b.Status == "COMPLETED");
            ViewBag.WaitingCheckInCount = bookingsToday.Count(b => b.Status == "CONFIRMED" || b.Status == "PENDING_VERIFICATION");
            ViewBag.MaintenanceCount = activeBlocks.Select(b => b.CourtId).Distinct().Count();

            // คิว Walk-in รอเรียกวันนี้
            ViewBag.WalkInWaitingCount = await _db.WalkInQueues
                .CountAsync(w => w.QueueDate == targetDate && (w.Status == "WAITING" || w.Status == "CALLED"));

            // ปัญหาอุปกรณ์ชำรุดที่ยังค้างอยู่
            ViewBag.PendingIssuesCount = await _db.CourtIssues
                .CountAsync(i => i.Status == "PENDING" || i.Status == "IN_PROGRESS");

            return View();
        }

        // ============================================================
        // 2. CHECK-IN / CHECK-OUT (อัปเดตสถานะ Check-in / Check-out)
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> CheckIn(uint bookingId)
        {
            try
            {
                var booking = await _db.Bookings
                    .Include(b => b.BookingSlots)
                    .FirstOrDefaultAsync(b => b.BookingId == bookingId);

                if (booking == null)
                {
                    return Json(new { success = false, message = "ไม่พบข้อมูลการจองในระบบ" });
                }

                uint staffId = await GetCurrentStaffIdAsync();

                booking.Status = "CHECKED_IN";
                booking.CheckedInAt = DateTime.Now;
                booking.CheckedInBy = staffId;
                booking.UpdatedAt = DateTime.Now;

                // เปิดไฟและพัดลมของคอร์ทที่จองโดยอัตโนมัติเพื่อเตรียมความพร้อม
                var affectedCourts = booking.BookingSlots.Select(bs => bs.CourtId).Distinct().ToList();
                foreach (var cId in affectedCourts)
                {
                    var state = _courtAmenities.GetOrAdd(cId, _ => new CourtAmenityState());
                    state.LightsOn = true;
                    state.FansOn = true;
                    state.LightsTurnedOnAt = DateTime.Now;
                    state.FansTurnedOnAt = DateTime.Now;
                }

                await _db.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"เช็คอินสำเร็จ! ระบบเปิดไฟและพัดลมสำหรับ {string.Join(", ", affectedCourts.Select(id => $"Court {id}"))} เรียบร้อยแล้ว",
                    checkedInTime = booking.CheckedInAt?.ToString("HH:mm:ss"),
                    affectedCourts = affectedCourts
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "เกิดข้อผิดพลาดในการ Check-in: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CheckOut(uint bookingId)
        {
            try
            {
                var booking = await _db.Bookings
                    .Include(b => b.BookingSlots)
                    .Include(b => b.BookingItems)
                    .FirstOrDefaultAsync(b => b.BookingId == bookingId);

                if (booking == null)
                {
                    return Json(new { success = false, message = "ไม่พบข้อมูลการจองในระบบ" });
                }

                uint staffId = await GetCurrentStaffIdAsync();

                booking.Status = "COMPLETED";
                booking.CheckedOutAt = DateTime.Now;
                booking.CheckedOutBy = staffId;
                booking.UpdatedAt = DateTime.Now;

                await _db.SaveChangesAsync();

                // ตรวจสอบว่ามีอุปกรณ์เช่าที่ต้องตรวจรับคืนหรือไม่
                int rentalItemsCount = booking.BookingItems.Count(bi => bi.ItemType == "RENT");

                var affectedCourts = booking.BookingSlots.Select(bs => bs.CourtId).Distinct().ToList();

                return Json(new
                {
                    success = true,
                    message = "Check-out เรียบร้อยแล้ว! ขอบคุณที่ใช้บริการ",
                    checkedOutTime = booking.CheckedOutAt?.ToString("HH:mm:ss"),
                    rentalItemsCount = rentalItemsCount,
                    affectedCourts = affectedCourts,
                    reminder = rentalItemsCount > 0
                        ? $"ลูกค้ามีรายการเช่าอุปกรณ์ {rentalItemsCount} ชิ้น กรุณาตรวจสอบการคืนอุปกรณ์ที่เคาน์เตอร์"
                        : "อย่าลืมตรวจสอบความเรียบร้อยและปิดไฟ/พัดลมหากไม่มีคิวต่อไป"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "เกิดข้อผิดพลาดในการ Check-out: " + ex.Message });
            }
        }

        // ============================================================
        // 3. TOGGLE COURT AMENITIES (ควบคุมสวิตช์ไฟ / พัดลม)
        // ============================================================
        [HttpPost]
        public IActionResult ToggleAmenity(ushort courtId, string amenityType, bool? state)
        {
            try
            {
                var amenityState = _courtAmenities.GetOrAdd(courtId, _ => new CourtAmenityState());
                string typeLower = amenityType?.ToLower() ?? "";

                if (typeLower == "light" || typeLower == "lights")
                {
                    amenityState.LightsOn = state ?? !amenityState.LightsOn;
                    amenityState.LightsTurnedOnAt = amenityState.LightsOn ? DateTime.Now : null;
                    return Json(new
                    {
                        success = true,
                        courtId = courtId,
                        amenity = "light",
                        isOn = amenityState.LightsOn,
                        message = amenityState.LightsOn ? $"เปิดไฟส่องสว่าง Court {courtId} เรียบร้อย" : $"ปิดไฟ Court {courtId} แล้ว"
                    });
                }
                else if (typeLower == "fan" || typeLower == "fans")
                {
                    amenityState.FansOn = state ?? !amenityState.FansOn;
                    amenityState.FansTurnedOnAt = amenityState.FansOn ? DateTime.Now : null;
                    return Json(new
                    {
                        success = true,
                        courtId = courtId,
                        amenity = "fan",
                        isOn = amenityState.FansOn,
                        message = amenityState.FansOn ? $"เปิดพัดลมระบายอากาศ Court {courtId} เรียบร้อย" : $"ปิดพัดลม Court {courtId} แล้ว"
                    });
                }

                return Json(new { success = false, message = "ประเภทอุปกรณ์ไม่ถูกต้อง" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ============================================================
        // 4. WALK-IN QUEUE MANAGEMENT (จัดการคิว Walk-in)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> WalkIn(string? date)
        {
            ViewData["Title"] = "จัดการคิว Walk-in ประจำวัน";
            await EnsureCourtStaffRoleAndUserAsync();

            string selectedDateStr = string.IsNullOrWhiteSpace(date)
                ? DateTime.Today.ToString("yyyy-MM-dd")
                : date.Trim();

            if (!DateOnly.TryParse(selectedDateStr, out DateOnly targetDate))
            {
                targetDate = DateOnly.FromDateTime(DateTime.Today);
                selectedDateStr = targetDate.ToString("yyyy-MM-dd");
            }

            ViewBag.SelectedDate = selectedDateStr;

            var queues = await _db.WalkInQueues
                .Include(q => q.Court)
                .Include(q => q.Slot)
                .Include(q => q.HandledByNavigation)
                .Where(q => q.QueueDate == targetDate)
                .OrderBy(q => q.QueueNo)
                .ToListAsync();

            ViewBag.Courts = await _db.Courts.OrderBy(c => c.CourtId).ToListAsync();
            ViewBag.TimeSlots = await _db.TimeSlots
                .Where(s => s.IsActive == true || s.IsActive == null)
                .OrderBy(s => s.SlotId)
                .ToListAsync();

            return View(queues);
        }

        [HttpPost]
        public async Task<IActionResult> AddWalkIn(string customerName, string? phone, ushort? courtId, byte? slotId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(customerName))
                {
                    TempData["ErrorMessage"] = "กรุณากรอกชื่อลูกค้า Walk-in";
                    return RedirectToAction("WalkIn");
                }

                DateOnly today = DateOnly.FromDateTime(DateTime.Today);

                // หาคิวลำดับถัดไปของวันนี้
                var maxNo = await _db.WalkInQueues
                    .Where(q => q.QueueDate == today)
                    .Select(q => (int?)q.QueueNo)
                    .MaxAsync() ?? 0;

                uint staffId = await GetCurrentStaffIdAsync();

                var newQueue = new WalkInQueue
                {
                    CustomerName = customerName.Trim(),
                    Phone = phone?.Trim(),
                    QueueDate = today,
                    CourtId = courtId > 0 ? courtId : null,
                    SlotId = slotId > 0 ? slotId : null,
                    QueueNo = (ushort)(maxNo + 1),
                    Status = "WAITING",
                    HandledBy = staffId,
                    CreatedAt = DateTime.Now
                };

                _db.WalkInQueues.Add(newQueue);
                await _db.SaveChangesAsync();

                TempData["SuccessMessage"] = $"ออกบัตรคิว Walk-in ลำดับที่ W-{newQueue.QueueNo:D2} ให้คุณ {newQueue.CustomerName} สำเร็จ!";
                return RedirectToAction("WalkIn");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "เกิดข้อผิดพลาดในการเพิ่มคิว Walk-in: " + ex.Message;
                return RedirectToAction("WalkIn");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQueueStatus(uint queueId, string status, ushort? courtId)
        {
            try
            {
                var queue = await _db.WalkInQueues.FindAsync(queueId);
                if (queue == null)
                {
                    return Json(new { success = false, message = "ไม่พบคิวที่ระบุ" });
                }

                queue.Status = status.ToUpper();
                if (courtId.HasValue && courtId > 0)
                {
                    queue.CourtId = courtId.Value;
                }

                await _db.SaveChangesAsync();

                string statusThai = queue.Status switch
                {
                    "CALLED" => "เรียกคิวแล้ว",
                    "ASSIGNED" => "จัดคอร์ทเข้าเล่นแล้ว",
                    "CANCELLED" => "ยกเลิกคิวแล้ว",
                    _ => "รอเรียกคิว"
                };

                return Json(new { success = true, message = $"อัปเดตสถานะคิว W-{queue.QueueNo:D2} เป็น '{statusThai}' เรียบร้อย" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ============================================================
        // 5. MAINTENANCE & COURT BLOCKING (ล็อกคอร์ทชั่วคราวกรณีซ่อมบำรุง)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Maintenance()
        {
            ViewData["Title"] = "ล็อกคอร์ทชั่วคราวกรณีซ่อมบำรุง";
            await EnsureCourtStaffRoleAndUserAsync();

            var blocks = await _db.CourtBlocks
                .Include(cb => cb.Court)
                .Include(cb => cb.CreatedByNavigation)
                .OrderByDescending(cb => cb.BlockStart)
                .ToListAsync();

            ViewBag.Courts = await _db.Courts.OrderBy(c => c.CourtId).ToListAsync();

            return View(blocks);
        }

        [HttpPost]
        public async Task<IActionResult> AddCourtBlock(ushort courtId, DateTime blockStart, DateTime blockEnd, string reason, bool setCourtMaintenance)
        {
            try
            {
                if (blockEnd <= blockStart)
                {
                    TempData["ErrorMessage"] = "เวลาสิ้นสุดการล็อกคอร์ทต้องอยู่หลังเวลาเริ่มต้น";
                    return RedirectToAction("Maintenance");
                }

                if (string.IsNullOrWhiteSpace(reason))
                {
                    TempData["ErrorMessage"] = "กรุณาระบุเหตุผลในการล็อกคอร์ท/ซ่อมบำรุง";
                    return RedirectToAction("Maintenance");
                }

                uint staffId = await GetCurrentStaffIdAsync();

                var block = new CourtBlock
                {
                    CourtId = courtId,
                    BlockStart = blockStart,
                    BlockEnd = blockEnd,
                    Reason = reason.Trim(),
                    CreatedBy = staffId,
                    CreatedAt = DateTime.Now
                };

                _db.CourtBlocks.Add(block);

                var court = await _db.Courts.FindAsync(courtId);
                if (court != null && setCourtMaintenance)
                {
                    court.Status = "MAINTENANCE";
                    court.UpdatedAt = DateTime.Now;
                }

                await _db.SaveChangesAsync();

                TempData["SuccessMessage"] = $"บันทึกล็อกคอร์ท {(court?.CourtName ?? $"Court {courtId}")} ช่วง {blockStart:dd/MM HH:mm} ถึง {blockEnd:dd/MM HH:mm} สำเร็จ!";
                return RedirectToAction("Maintenance");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "เกิดข้อผิดพลาดในการล็อกคอร์ท: " + ex.Message;
                return RedirectToAction("Maintenance");
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveCourtBlock(uint blockId, bool restoreCourtAvailable)
        {
            try
            {
                var block = await _db.CourtBlocks.FindAsync(blockId);
                if (block == null)
                {
                    TempData["ErrorMessage"] = "ไม่พบรายการล็อกคอร์ท";
                    return RedirectToAction("Maintenance");
                }

                ushort courtId = block.CourtId;
                _db.CourtBlocks.Remove(block);

                if (restoreCourtAvailable)
                {
                    var court = await _db.Courts.FindAsync(courtId);
                    if (court != null)
                    {
                        court.Status = "AVAILABLE";
                        court.UpdatedAt = DateTime.Now;
                    }
                }

                await _db.SaveChangesAsync();

                TempData["SuccessMessage"] = "ปลดล็อกคอร์ทเรียบร้อยแล้ว สนามพร้อมเปิดให้บริการตามปกติ";
                return RedirectToAction("Maintenance");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "เกิดข้อผิดพลาดในการปลดล็อกคอร์ท: " + ex.Message;
                return RedirectToAction("Maintenance");
            }
        }

        // ============================================================
        // 6. REPORT COURT ISSUES TO ADMIN (แจ้ง Admin หากอุปกรณ์ในสนามชำรุด)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Issues()
        {
            ViewData["Title"] = "แจ้ง Admin กรณีอุปกรณ์ในสนามชำรุด";
            await EnsureCourtStaffRoleAndUserAsync();

            var issues = await _db.CourtIssues
                .Include(i => i.Court)
                .Include(i => i.ReportedByNavigation)
                .Include(i => i.ResolvedByNavigation)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            ViewBag.Courts = await _db.Courts.OrderBy(c => c.CourtId).ToListAsync();

            return View(issues);
        }

        [HttpPost]
        public async Task<IActionResult> ReportIssue(ushort? courtId, string description)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(description))
                {
                    TempData["ErrorMessage"] = "กรุณาระบุรายละเอียดอุปกรณ์หรือสนามที่ชำรุด";
                    return RedirectToAction("Issues");
                }

                uint staffId = await GetCurrentStaffIdAsync();

                var issue = new CourtIssue
                {
                    CourtId = courtId > 0 ? courtId : null,
                    ReportedBy = staffId,
                    Description = description.Trim(),
                    Status = "PENDING",
                    CreatedAt = DateTime.Now
                };

                _db.CourtIssues.Add(issue);
                await _db.SaveChangesAsync();

                TempData["SuccessMessage"] = "ส่งรายงานอุปกรณ์ชำรุดไปยัง Admin สำเร็จเรียบร้อย! Admin จะได้รับการแจ้งเตือนทันที";
                return RedirectToAction("Issues");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "เกิดข้อผิดพลาดในการแจ้งเรื่อง: " + ex.Message;
                return RedirectToAction("Issues");
            }
        }

        // ============================================================
        // PRIVATE HELPER METHODS
        // ============================================================
        private async Task<uint> GetCurrentStaffIdAsync()
        {
            string? uidStr = HttpContext.Session.GetString("UserId") ?? Request.Cookies["cb_user_id"];
            if (!string.IsNullOrEmpty(uidStr) && uint.TryParse(uidStr, out uint uid))
            {
                var existingUser = await _db.Users.FindAsync(uid);
                if (existingUser != null) return uid;
            }

            var supervisor = await _db.Users.FirstOrDefaultAsync(u => u.Role.RoleCode == "COURT_STAFF");
            if (supervisor != null) return supervisor.UserId;

            var firstUser = await _db.Users.FirstOrDefaultAsync();
            return firstUser?.UserId ?? 1;
        }

        private async Task EnsureCourtStaffRoleAndUserAsync()
        {
            // ตรวจสอบ Role COURT_STAFF ในตาราง roles (ไม่สร้าง User ซ้ำอัตโนมัติ)
            var courtRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleCode == "COURT_STAFF");
            if (courtRole == null)
            {
                courtRole = new Role
                {
                    RoleCode = "COURT_STAFF",
                    RoleName = "ฝ่ายดูแลคอร์ท",
                    Description = "ดูตารางการจองประจำวัน ควบคุมไฟ/พัดลม เช็คอิน/เช็คเอาท์ จัดการ Walk-in ล็อกคอร์ทซ่อมบำรุง และแจ้งอุปกรณ์ชำรุด"
                };
                _db.Roles.Add(courtRole);
                await _db.SaveChangesAsync();
            }
        }
    }
}