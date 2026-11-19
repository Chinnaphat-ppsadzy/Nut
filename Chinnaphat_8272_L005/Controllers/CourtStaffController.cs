using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Chinnaphat_8272_L005.Models;
using Chinnaphat_8272_L005.Helpers;

namespace Chinnaphat_8272_L005.Controllers
{
    public class CourtAmenityState
    {
        public bool LightsOn { get; set; } = false;
        public bool FansOn { get; set; } = false;
        public DateTime? LightsTurnedOnAt { get; set; }
        public DateTime? FansTurnedOnAt { get; set; }
    }

    [RequireRole("COURT_STAFF", "ADMIN")]
    public class CourtStaffController : Controller
    {
        private readonly Csi402dbContext _db;
        private readonly IWebHostEnvironment _env;

        // In-memory thread-safe state for Court Amenities (Lights & Fans)
        private static readonly ConcurrentDictionary<ushort, CourtAmenityState> _courtAmenities = new();

        public CourtStaffController(Csi402dbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // ============================================================
        // 1. DAILY SCHEDULE & AMENITY CONTROL (ดูตารางการจองประจำวัน & ควบคุมไฟ/พัดลม)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Index(string? date)
        {
            ViewData["Title"] = "ตารางการจองคอร์ทประจำวัน";
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
                .Where(b => b.PlayDate == targetDate && b.Status != "CANCELLED" && b.Status != "REJECTED" && b.Status != "EXPIRED")
                .OrderBy(b => b.CreatedAt)
                .ToListAsync();

            ViewBag.BookingsToday = bookingsToday;

            // Walk-in ที่ระบุคอร์ท/เวลาไว้ต้องแสดงเป็นคิวที่จองใน Timeline ด้วย
            var walkInsForDate = await _db.WalkInQueues
                .Include(q => q.Court)
                .Include(q => q.Slot)
                .Where(q => q.QueueDate == targetDate && q.Status != "CANCELLED")
                .OrderBy(q => q.QueueNo)
                .ToListAsync();
            var scheduledWalkIns = walkInsForDate
                .Where(q => q.CourtId.HasValue && q.SlotId.HasValue)
                .ToList();
            ViewBag.ScheduledWalkIns = scheduledWalkIns;
            ViewBag.WalkInWaitingCount = walkInsForDate.Count(q => q.Status == "WAITING");

            // ดึงรายการบล็อกคอร์ท (Court Blocks) ที่ครอบคลุมวันที่เลือก
            DateTime dayStart = targetDate.ToDateTime(TimeOnly.MinValue);
            DateTime dayEnd = targetDate.ToDateTime(TimeOnly.MaxValue);

            var activeBlocks = await _db.CourtBlocks
                .Include(cb => cb.Court)
                .Include(cb => cb.CreatedByNavigation)
                .Where(cb => cb.BlockStart <= dayEnd && cb.BlockEnd >= dayStart)
                .ToListAsync();

            ViewBag.ActiveBlocks = activeBlocks;

            // สถิติด่วนประจำวัน
            ViewBag.TotalBookingsCount = bookingsToday.Count + scheduledWalkIns.Count;
            ViewBag.CheckedInCount = bookingsToday.Count(b => b.Status == "CHECKED_IN" || b.Status == "IN_USE");
            ViewBag.CompletedCount = bookingsToday.Count(b => b.Status == "COMPLETED");
            ViewBag.WaitingCheckInCount = bookingsToday.Count(b => b.Status == "CONFIRMED" || b.Status == "PENDING_VERIFY");
            ViewBag.MaintenanceCount = activeBlocks.Select(b => b.CourtId).Distinct().Count();

            // ปัญหาอุปกรณ์ชำรุดที่ยังค้างอยู่
            ViewBag.PendingIssuesCount = await _db.CourtIssues
                .CountAsync(i => i.Status == "OPEN" || i.Status == "IN_PROGRESS");

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
            ViewBag.CanIssueWalkIn = targetDate == DateOnly.FromDateTime(DateTime.Today);

            var queues = await _db.WalkInQueues
                .Include(q => q.Court)
                .Include(q => q.Slot)
                .Include(q => q.HandledByNavigation)
                .Where(q => q.QueueDate == targetDate)
                .OrderBy(q => q.QueueNo)
                .ToListAsync();

            ViewBag.Courts = await _db.Courts.OrderBy(c => c.CourtId).ToListAsync();
            ViewBag.TimeSlots = await _db.TimeSlots
                .Where(s => (s.IsActive == true || s.IsActive == null) && s.StartTime >= TimeOnly.FromDateTime(DateTime.Now))
                .OrderBy(s => s.SlotId)
                .ToListAsync();
            ViewBag.PaidWalkInTotal = queues.Where(q => q.PaymentStatus == "PAID").Sum(q => q.CourtFee);
            ViewBag.UnpaidWalkInCount = queues.Count(q => q.Status == "ASSIGNED" && q.PaymentStatus != "PAID");

            return View(queues);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddWalkIn(string customerName, string? phone, ushort? courtId, byte? slotId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(customerName) || customerName.Trim().Length > 150)
                {
                    TempData["ErrorMessage"] = "กรุณากรอกชื่อลูกค้า Walk-in ไม่เกิน 150 ตัวอักษร";
                    return RedirectToAction("WalkIn");
                }
                if (string.IsNullOrWhiteSpace(phone) || phone.Trim().Length != 10 || phone.Trim().Any(c => c < '0' || c > '9'))
                {
                    TempData["ErrorMessage"] = "กรุณากรอกเบอร์โทรศัพท์เป็นตัวเลขให้ครบ 10 หลัก";
                    return RedirectToAction("WalkIn");
                }

                if (courtId.HasValue && !await _db.Courts.AnyAsync(c => c.CourtId == courtId.Value && c.Status == "AVAILABLE"))
                {
                    TempData["ErrorMessage"] = "คอร์ทที่เลือกปิดให้บริการอยู่ กรุณาเลือกคอร์ทอื่น";
                    return RedirectToAction("WalkIn");
                }
                if (slotId.HasValue && !await _db.TimeSlots.AnyAsync(s => s.SlotId == slotId.Value &&
                    (s.IsActive == true || s.IsActive == null) && s.StartTime >= TimeOnly.FromDateTime(DateTime.Now)))
                {
                    TempData["ErrorMessage"] = "ช่วงเวลาที่เลือกไม่เปิดให้บริการ กรุณาเลือกเวลาใหม่";
                    return RedirectToAction("WalkIn");
                }

                DateOnly today = DateOnly.FromDateTime(DateTime.Today);

                if (courtId.HasValue && slotId.HasValue)
                {
                    var requestedSlot = await _db.TimeSlots.FindAsync(slotId.Value);
                    var requestedStart = today.ToDateTime(requestedSlot!.StartTime);
                    var requestedEnd = today.ToDateTime(requestedSlot.EndTime);
                    bool hasOnlineBooking = await _db.BookingSlots
                        .AnyAsync(bs => bs.PlayDate == today && bs.CourtId == courtId.Value && bs.SlotId == slotId.Value && bs.LockKey == 1 &&
                            bs.Booking.Status != "CANCELLED" && bs.Booking.Status != "REJECTED" && bs.Booking.Status != "EXPIRED");
                    bool hasMaintenanceBlock = await _db.CourtBlocks.AnyAsync(b =>
                        b.CourtId == courtId.Value && b.BlockStart < requestedEnd && b.BlockEnd > requestedStart);
                    bool hasWalkInReservation = await _db.WalkInQueues.AnyAsync(q =>
                        q.QueueDate == today && q.Status != "CANCELLED" && q.CourtId == courtId.Value && q.SlotId == slotId.Value);
                    if (hasOnlineBooking || hasMaintenanceBlock || hasWalkInReservation)
                    {
                        TempData["ErrorMessage"] = "คอร์ท/เวลาที่เลือกถูกจองหรือล็อกไว้แล้ว กรุณาเลือกเวลาอื่น";
                        return RedirectToAction("WalkIn");
                    }
                }

                // หาคิวลำดับถัดไปของวันนี้
                var maxNo = await _db.WalkInQueues
                    .Where(q => q.QueueDate == today)
                    .Select(q => (int?)q.QueueNo)
                    .MaxAsync() ?? 0;

                uint staffId = await GetCurrentStaffIdAsync();

                var newQueue = new WalkInQueue
                {
                    CustomerName = customerName.Trim(),
                    Phone = phone!.Trim(),
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQueueStatus(uint queueId, string status)
        {
            try
            {
                var queue = await _db.WalkInQueues.FindAsync(queueId);
                if (queue == null)
                {
                    return Json(new { success = false, message = "ไม่พบคิวที่ระบุ" });
                }

                status = (status ?? string.Empty).Trim().ToUpperInvariant();
                if (status is not ("ASSIGNED" or "DONE" or "CANCELLED"))
                {
                    return Json(new { success = false, message = "สถานะคิวไม่ถูกต้อง" });
                }

                if (status == "ASSIGNED")
                {
                    if (queue.Status is "CANCELLED" or "DONE")
                        return Json(new { success = false, message = "คิวนี้ปิดรายการแล้ว ไม่สามารถจัดคอร์ทซ้ำได้" });

                    var courts = await _db.Courts
                        .Where(c => c.Status == "AVAILABLE" && (!queue.CourtId.HasValue || c.CourtId == queue.CourtId.Value))
                        .OrderBy(c => c.CourtId)
                        .ToListAsync();
                    var slots = await _db.TimeSlots
                        .Where(s => (s.IsActive == true || s.IsActive == null) &&
                            (!queue.SlotId.HasValue || s.SlotId == queue.SlotId.Value))
                        .OrderBy(s => s.StartTime)
                        .ToListAsync();

                    var now = DateTime.Now;
                    if (queue.QueueDate == DateOnly.FromDateTime(now))
                        slots = slots.Where(s => s.StartTime >= TimeOnly.FromDateTime(now)).ToList();
                    if (!courts.Any() || !slots.Any())
                        return Json(new { success = false, message = "ไม่มีคอร์ทหรือช่วงเวลาที่พร้อมให้บริการตามตัวเลือก กรุณาแก้ตัวเลือกคิวแล้วลองใหม่" });

                    var occupiedBookings = await _db.BookingSlots
                        .Include(s => s.Booking)
                        .Include(s => s.Slot)
                        .Where(s => s.PlayDate == queue.QueueDate && s.LockKey == 1 &&
                            s.Booking.Status != "CANCELLED" && s.Booking.Status != "REJECTED" && s.Booking.Status != "EXPIRED")
                        .ToListAsync();
                    var dayStart = queue.QueueDate.ToDateTime(TimeOnly.MinValue);
                    var dayEnd = queue.QueueDate.AddDays(1).ToDateTime(TimeOnly.MinValue);
                    var blocks = await _db.CourtBlocks
                        .Where(b => b.BlockStart < dayEnd && b.BlockEnd > dayStart)
                        .ToListAsync();
                    var assignedWalkIns = await _db.WalkInQueues
                        .Where(q => q.QueueId != queue.QueueId && q.QueueDate == queue.QueueDate &&
                            q.Status != "CANCELLED" && q.CourtId.HasValue && q.SlotId.HasValue)
                        .Select(q => new { q.CourtId, q.SlotId })
                        .ToListAsync();

                    Court? chosenCourt = null;
                    TimeSlot? chosenSlot = null;
                    foreach (var candidateCourt in courts)
                    {
                        foreach (var candidateSlot in slots)
                        {
                            var start = queue.QueueDate.ToDateTime(candidateSlot.StartTime);
                            var end = queue.QueueDate.ToDateTime(candidateSlot.EndTime);
                            bool conflictsWithBooking = occupiedBookings.Any(b => b.CourtId == candidateCourt.CourtId &&
                                queue.QueueDate.ToDateTime(b.Slot.StartTime) < end && queue.QueueDate.ToDateTime(b.Slot.EndTime) > start);
                            bool conflictsWithBlock = blocks.Any(b => b.CourtId == candidateCourt.CourtId && b.BlockStart < end && b.BlockEnd > start);
                            bool conflictsWithWalkIn = assignedWalkIns.Any(q => q.CourtId == candidateCourt.CourtId && q.SlotId == candidateSlot.SlotId);
                            if (!conflictsWithBooking && !conflictsWithBlock && !conflictsWithWalkIn)
                            {
                                chosenCourt = candidateCourt;
                                chosenSlot = candidateSlot;
                                break;
                            }
                        }
                        if (chosenCourt != null) break;
                    }

                    if (chosenCourt == null || chosenSlot == null)
                        return Json(new { success = false, message = "ไม่พบคอร์ทว่างในช่วงเวลาที่เหลือ กรุณาเลือกเวลาอื่น" });

                    var durationHours = (decimal)(chosenSlot.EndTime.ToTimeSpan() - chosenSlot.StartTime.ToTimeSpan()).TotalMinutes / 60m;
                    if (durationHours <= 0)
                        return Json(new { success = false, message = "ช่วงเวลาคอร์ทไม่ถูกต้อง" });
                    queue.CourtId = chosenCourt.CourtId;
                    queue.SlotId = chosenSlot.SlotId;
                    queue.CourtFee = Math.Round(chosenCourt.PricePerHour * durationHours, 2);
                }
                else if (status == "DONE" && queue.PaymentStatus != "PAID")
                {
                    return Json(new { success = false, message = "กรุณาบันทึกการชำระเงินก่อนปิดคิว" });
                }
                else if (status == "CANCELLED" && queue.PaymentStatus == "PAID")
                {
                    return Json(new { success = false, message = "คิวนี้ชำระเงินแล้ว กรุณาดำเนินการคืนเงินก่อนยกเลิก" });
                }

                queue.Status = status;

                await _db.SaveChangesAsync();

                string statusThai = queue.Status switch
                {
                    "ASSIGNED" => $"จัด {queue.Court?.CourtName ?? $"Court {queue.CourtId}"} และล็อกราคา {queue.CourtFee:N0} บาทแล้ว",
                    "DONE" => "ปิดคิวแล้ว",
                    "CANCELLED" => "ยกเลิกคิวแล้ว",
                    _ => "รอจัดคอร์ท"
                };

                return Json(new { success = true, message = $"อัปเดตสถานะคิว W-{queue.QueueNo:D2} เป็น '{statusThai}' เรียบร้อย" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadWalkInSlip(uint queueId, IFormFile slipFile)
        {
            var queue = await _db.WalkInQueues.FindAsync(queueId);
            if (queue == null)
                return Json(new { success = false, message = "ไม่พบคิวที่ระบุ" });
            if (queue.Status != "ASSIGNED" || queue.CourtFee <= 0)
                return Json(new { success = false, message = "กรุณาจัดคอร์ทให้คิวนี้ก่อนชำระเงิน" });
            if (queue.PaymentStatus == "PAID")
                return Json(new { success = false, message = "คิวนี้ได้รับการยืนยันชำระเงินแล้ว" });
            var extension = await GetSlipImageExtensionAsync(slipFile);
            if (extension == null)
                return Json(new { success = false, message = "กรุณาแนบสลิป JPG, PNG หรือ WEBP ขนาดไม่เกิน 5 MB" });

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "slips");
            Directory.CreateDirectory(uploadsFolder);
            var safeName = $"walkin_W{queue.QueueNo:D2}_{queue.QueueDate:yyyyMMdd}_{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(uploadsFolder, safeName);
            await using (var stream = new FileStream(fullPath, FileMode.CreateNew))
                await slipFile.CopyToAsync(stream);

            queue.PaymentStatus = "PENDING_VERIFY";
            queue.PaymentMethod = "QR_TRANSFER";
            queue.SlipImageUrl = $"/uploads/slips/{safeName}";
            queue.PaidAt = null;
            queue.PaidBy = null;
            await _db.SaveChangesAsync();

            return Json(new { success = true, message = $"บันทึกสลิปคิว W-{queue.QueueNo:D2} แล้ว ส่งให้ฝ่ายบัญชีตรวจสอบยอด {queue.CourtFee:N2} บาท" });
        }

        private static async Task<string?> GetSlipImageExtensionAsync(IFormFile? file)
        {
            if (file == null || file.Length <= 0 || file.Length > 5 * 1024 * 1024) return null;
            var header = new byte[12];
            await using var stream = file.OpenReadStream();
            var read = await stream.ReadAsync(header.AsMemory(0, header.Length));
            if (read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF) return ".jpg";
            if (read >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })) return ".png";
            if (read >= 12 && header[0] == 'R' && header[1] == 'I' && header[2] == 'F' && header[3] == 'F' &&
                header[8] == 'W' && header[9] == 'E' && header[10] == 'B' && header[11] == 'P') return ".webp";
            return null;
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCourtBlock(ushort courtId, DateTime blockStart, DateTime blockEnd, string reason, bool setCourtMaintenance)
        {
            try
            {
                var court = await _db.Courts.FindAsync(courtId);
                if (court == null)
                {
                    TempData["ErrorMessage"] = "ไม่พบคอร์ทที่เลือก กรุณาเลือกสนามใหม่";
                    return RedirectToAction("Maintenance");
                }

                if (blockStart < DateTime.Now)
                {
                    TempData["ErrorMessage"] = "เวลาเริ่มล็อกต้องไม่เป็นเวลาในอดีต";
                    return RedirectToAction("Maintenance");
                }

                if (blockEnd <= blockStart)
                {
                    TempData["ErrorMessage"] = "เวลาสิ้นสุดการล็อกคอร์ทต้องอยู่หลังเวลาเริ่มต้น";
                    return RedirectToAction("Maintenance");
                }

                if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 255)
                {
                    TempData["ErrorMessage"] = "กรุณาระบุเหตุผลในการล็อกคอร์ทไม่เกิน 255 ตัวอักษร";
                    return RedirectToAction("Maintenance");
                }

                bool overlapsExistingBlock = await _db.CourtBlocks.AnyAsync(b =>
                    b.CourtId == courtId && b.BlockStart < blockEnd && b.BlockEnd > blockStart);
                if (overlapsExistingBlock)
                {
                    TempData["ErrorMessage"] = "ช่วงเวลานี้มีรายการล็อกคอร์ทอยู่แล้ว กรุณาเลือกเวลาใหม่";
                    return RedirectToAction("Maintenance");
                }

                var firstDate = DateOnly.FromDateTime(blockStart);
                var lastDate = DateOnly.FromDateTime(blockEnd.AddTicks(-1));
                var existingBookings = await _db.BookingSlots
                    .Include(bs => bs.Booking)
                    .Include(bs => bs.Slot)
                    .Where(bs => bs.CourtId == courtId && bs.PlayDate >= firstDate && bs.PlayDate <= lastDate &&
                        bs.LockKey == 1 && bs.Booking.Status != "CANCELLED" && bs.Booking.Status != "REJECTED" && bs.Booking.Status != "EXPIRED")
                    .ToListAsync();
                bool overlapsBooking = existingBookings.Any(bs =>
                {
                    var slotStart = bs.PlayDate.ToDateTime(bs.Slot.StartTime);
                    var slotEnd = bs.PlayDate.ToDateTime(bs.Slot.EndTime);
                    return slotStart < blockEnd && slotEnd > blockStart;
                });
                if (overlapsBooking)
                {
                    TempData["ErrorMessage"] = "ช่วงเวลานี้มีลูกค้าจองคอร์ทแล้ว ไม่สามารถล็อกทับได้";
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
        [ValidateAntiForgeryToken]
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
                    var now = DateTime.Now;
                    bool hasOtherActiveBlock = await _db.CourtBlocks.AnyAsync(b =>
                        b.BlockId != blockId && b.CourtId == courtId && b.BlockStart <= now && b.BlockEnd > now);
                    bool hasActiveRepairIssue = await _db.CourtIssues.AnyAsync(i =>
                        i.CourtId == courtId && (i.Status == "OPEN" || i.Status == "IN_PROGRESS"));
                    if (court != null && !hasOtherActiveBlock && !hasActiveRepairIssue)
                    {
                        court.Status = "AVAILABLE";
                        court.UpdatedAt = DateTime.Now;
                    }
                }

                await _db.SaveChangesAsync();

                TempData["SuccessMessage"] = "ปลดล็อกรายการเรียบร้อยแล้ว";
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportIssue(ushort? courtId, string description)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(description) || description.Trim().Length > 500)
                {
                    TempData["ErrorMessage"] = "กรุณาระบุรายละเอียดอุปกรณ์หรือสนามที่ชำรุด (ไม่เกิน 500 ตัวอักษร)";
                    return RedirectToAction("Issues");
                }

                uint staffId = await GetCurrentStaffIdAsync();

                var issue = new CourtIssue
                {
                    CourtId = courtId > 0 ? courtId : null,
                    ReportedBy = staffId,
                    Description = description.Trim(),
                    Status = "OPEN",
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
