using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Chinnaphat_8272_L005.Models;
using Chinnaphat_8272_L005.ViewModels;
using System.ComponentModel.Design;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.IO;
using Chinnaphat_8272_L005.Helpers;

namespace Chinnaphat_8272_L005.Controllers;

public class HomeController : Controller
{
    private readonly Csi402dbContext _db;
    private readonly IWebHostEnvironment _env;

    public HomeController (Csi402dbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public IActionResult Lab9()
    {
        //Create || Insert Value in SQL
        return View();
    }   
    [HttpPost]
    public IActionResult Lab9(Labstudent data)
    {
        if (data.StdPassword != null)
        {
            if (ModelState.IsValid)
            {
                var CheckStd = (from a in _db.Labstudents where a.StdId == data.StdId select a).FirstOrDefault();
                if (CheckStd == null)
                {
                    Labstudent s = new();
                    s.StdId = data.StdId;
                    s.StdPassword = data.StdPassword;
                    s.StdName = data.StdName;
                    s.StdLastname = data.StdLastname;
                    _db.Add(s);
                    _db.SaveChanges();
                    return View(data);
                }
                else
                {
                    ViewBag.Status = "StdCode ซํ้า";
                    return View(data);
                }
            }
            else
            {
                ViewBag.Status = "Error";
                return View(data);
            }
        }
        else
        {
            ViewBag.Status = "Password Null";
            return View(data);
        }
    }

    public IActionResult Lab9_1()
    {
        var Student = (from s in _db.Labstudents select s).ToList();
        return View(Student);
    }
    public IActionResult Lab10( string? StdId)
    {
        var User = (from s in _db.Labstudents where s.StdId == StdId select s).FirstOrDefault();
        return View(User);
    }
    [HttpPost]
    public IActionResult Lab10(Labstudent data)
    {
       var User = (from s in _db.Labstudents where s.StdId == data.StdId select s).FirstOrDefault();
           User.StdPassword = data.StdPassword;
           User.StdName = data.StdName;
           User.StdLastname = data.StdLastname;
           _db.SaveChanges();
       
       return View(User);
    }
    public IActionResult Lab10_1(string? StdId)
    {
        var Check = (from s in _db.Labstudents where s.StdId == StdId select s).FirstOrDefault();
        if(Check !=null)
        {
        _db.Remove(Check);
        _db.SaveChanges();
        return RedirectToAction("Lab9_1","Home",new{ status = "ลบสำเร็จ"});
        }
        else
        {
            return RedirectToAction("Lab9_1","Home",new{ status = "ลบไม่สำเร็จ"});
        }
    }

    public async Task<IActionResult> Index()
    {
        var userIdText = HttpContext.Session.GetString("UserId");
        if (uint.TryParse(userIdText, out var userId))
        {
            var roleCode = await _db.Users.Where(u => u.UserId == userId)
                .Select(u => u.Role.RoleCode).FirstOrDefaultAsync();
            return roleCode switch
            {
                "ACCOUNTING" => RedirectToAction("Index", "Accounting"),
                "SALES_STAFF" => RedirectToAction("Index", "SalesStaff"),
                "COURT_STAFF" => RedirectToAction("Index", "CourtStaff"),
                "ADMIN" => RedirectToAction("Admin", "Home"),
                _ => RedirectToAction(nameof(Customer))
            };
        }
        return RedirectToAction(nameof(Customer));
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public async Task<IActionResult> Customer(string? date, string? section, string? showpromo, string? code)
    {
        if (uint.TryParse(HttpContext.Session.GetString("UserId"), out var signedInUserId))
        {
            var roleCode = await _db.Users.Where(u => u.UserId == signedInUserId)
                .Select(u => u.Role.RoleCode).FirstOrDefaultAsync();
            var roleHome = roleCode switch
            {
                "ACCOUNTING" => Url.Action("Index", "Accounting"),
                "SALES_STAFF" => Url.Action("Index", "SalesStaff"),
                "COURT_STAFF" => Url.Action("Index", "CourtStaff"),
                "ADMIN" => Url.Action("Admin", "Home"),
                _ => null
            };
            if (roleHome != null) return Redirect(roleHome);
        }
        ViewBag.ShowPromoPopup = showpromo == "1";
        ViewBag.TrackingCode = code?.Trim();
        // Set section-specific title
        string sectionLower = section?.ToLower()?.Trim() ?? "";
        ViewBag.Section = sectionLower;
        ViewData["Title"] = sectionLower switch
        {
            "booking"    => "จองสนามแบดมินตัน",
            "product"    => "ซื้ออุปกรณ์ & เช่าอุปกรณ์",
            "tracking"   => "ติดตามสถานะการจอง",
            "mybookings" => "การจองของฉัน",
            _            => "หน้าหลัก - ระบบจองและซื้ออุปกรณ์"
        };
        var today = DateOnly.FromDateTime(DateTime.Today);
        var lastBookableDate = today.AddDays(7);
        var selectedDate = DateFormatHelper.TryParseIsoDate(date, out var requestedDate) && requestedDate >= today && requestedDate <= lastBookableDate
            ? DateFormatHelper.ToIsoDate(requestedDate)
            : DateFormatHelper.ToIsoDate(today);
        ViewBag.SelectedDate = selectedDate;

        try
        {
            var courts = await _db.Courts.Where(c => c.Status == "AVAILABLE").ToListAsync();
            if (!courts.Any())
            {
                courts = await _db.Courts.ToListAsync();
            }
            ViewBag.Courts = courts;

            var slots = await _db.TimeSlots
                                 .Where(s => s.IsActive == true || s.IsActive == null)
                                 .OrderBy(s => s.SlotId)
                                 .ToListAsync();
            ViewBag.TimeSlots = slots;

            // ดึงโปรโมชั่นที่เปิดใช้งานอยู่จากฐานข้อมูลหลังบ้าน
            var activePromos = await _db.Promotions
                                        .Include(p => p.PromotionRules)
                                        .Where(p => p.IsActive == true)
                                        .OrderBy(p => p.Priority)
                                        .ToListAsync();
            ViewBag.Promotions = activePromos;

            string? customerId = HttpContext.Session.GetString("UserId");
            ViewBag.IsFirstCourtBooking = uint.TryParse(customerId, out var customerUid) &&
                !await _db.Bookings.AnyAsync(b => b.UserId == customerUid &&
                    (b.Status == "CONFIRMED" || b.Status == "CHECKED_IN" || b.Status == "COMPLETED" ||
                     b.Payments.Any(p => p.Status == "APPROVED" || p.Status == "PAID")));

            // ดึงรายการสินค้าและอุปกรณ์สำหรับซื้อ/เช่าจากฐานข้อมูล
            var products = await _db.Products
                                   .Include(p => p.Category)
                                   .Where(p => p.IsActive == true || p.IsActive == null)
                                   .OrderBy(p => p.ProductType)
                                   .ThenBy(p => p.CategoryId)
                                   .ToListAsync();
            ViewBag.Products = products;

            // Show rental stock available for the selected play date, not total inventory.
            if (DateFormatHelper.TryParseIsoDate(selectedDate, out DateOnly rentalDate))
            {
                var rentalProductIds = products.Where(p => p.ProductType == "RENT").Select(p => p.ProductId).ToList();
                var reservedRentals = await _db.BookingItems
                    .Where(i => rentalProductIds.Contains(i.ProductId) && i.ItemType == "RENT" && i.Booking.PlayDate == rentalDate &&
                        (i.Booking.Status == "PENDING_PAYMENT" || i.Booking.Status == "PENDING_VERIFY" || i.Booking.Status == "CONFIRMED"))
                    .GroupBy(i => i.ProductId)
                    .Select(g => new { ProductId = g.Key, Quantity = g.Sum(i => (int)i.Quantity) })
                    .ToDictionaryAsync(g => g.ProductId, g => g.Quantity);

                ViewBag.RentalAvailabilityByProductId = products
                    .Where(p => p.ProductType == "RENT")
                    .ToDictionary(p => p.ProductId, p => Math.Max(0, p.StockQty - reservedRentals.GetValueOrDefault(p.ProductId)));
            }

            var bookedSlotKeys = new HashSet<string>();
            if (DateFormatHelper.TryParseIsoDate(selectedDate, out DateOnly targetDate))
            {
                var booked = await _db.BookingSlots
                                      .Include(bs => bs.Booking)
                                      .Where(bs => bs.PlayDate == targetDate && (bs.LockKey == 1 || (bs.Booking != null && bs.Booking.Status != "CANCELLED")))
                                      .Select(bs => $"{bs.CourtId}_{bs.SlotId}")
                                      .Distinct()
                                      .ToListAsync();
                bookedSlotKeys = new HashSet<string>(booked);
            }
            ViewBag.BookedSlots = bookedSlotKeys;

            // ตรวจสอบข้อมูลผู้ใช้งานที่ล็อกอิน
            string? loggedInUidStr = HttpContext.Session.GetString("UserId");
            if (!string.IsNullOrEmpty(loggedInUidStr) && uint.TryParse(loggedInUidStr, out uint currentUid))
            {
                var currentUser = await _db.Users.FindAsync(currentUid);
                if (currentUser != null)
                {
                    ViewBag.CurrentUserName = currentUser.FullName;
                    ViewBag.CurrentUserPhone = currentUser.Phone;
                    ViewBag.CurrentUserEmail = currentUser.Email;
                }
            }

            // ดึงประวัติการจองของฉัน (My Bookings) - เฉพาะสมาชิกที่เข้าสู่ระบบเท่านั้น
            // ในโหมด Guest จะไม่สามารถดูการจองของคนอื่นได้โดยเด็ดขาด
            var myBookingsList = new List<BookingTrackItem>();
            uint lUid = 0;
            bool isUserLoggedIn = !string.IsNullOrEmpty(loggedInUidStr) && uint.TryParse(loggedInUidStr, out lUid);

            if (isUserLoggedIn)
            {
                var rawBookings = await _db.Bookings
                    .Include(b => b.BookingSlots).ThenInclude(bs => bs.Court)
                    .Include(b => b.BookingSlots).ThenInclude(bs => bs.Slot)
                    .Include(b => b.BookingItems).ThenInclude(bi => bi.Product)
                    .Include(b => b.Payments)
                    .Include(b => b.User)
                    .Where(b => b.UserId == lUid)
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(50)
                    .ToListAsync();

                foreach (var b in rawBookings)
                {
                    bool hasCourtBooking = b.BookingSlots.Any();
                    var item = new BookingTrackItem
                    {
                        BookingId = b.BookingId,
                        BookingCode = b.BookingCode,
                        PlayDate = DateFormatHelper.ToIsoDate(b.PlayDate),
                        CustomerName = b.User?.FullName ?? "คุณลูกค้า",
                        CustomerPhone = b.User?.Phone ?? "-",
                        CourtAmount = b.CourtAmount,
                        SaleAmount = b.SaleAmount,
                        RentalAmount = b.RentalAmount,
                        DiscountAmount = b.DiscountAmount,
                        NetAmount = b.NetAmount,
                        Status = b.Status,
                        CreatedAt = b.CreatedAt,
                        SlipImageUrl = b.Payments.Any(p => !string.IsNullOrEmpty(p.SlipImageUrl))
                            ? Url.Action(nameof(ViewSlip), new { bookingCode = b.BookingCode }) : null
                    };

                    switch (b.Status?.ToUpper())
                    {
                        case "PENDING_VERIFY":
                            item.StatusText = "รอฝ่ายบัญชีตรวจสอบสลิป";
                            item.StatusBadgeClass = "bg-warning text-dark";
                            break;
                        case "CONFIRMED":
                            item.StatusText = hasCourtBooking
                                ? "อนุมัติการชำระเงินแล้ว (ยืนยันคิวสนามเรียบร้อย)"
                                : "อนุมัติการชำระเงินแล้ว (รอรับสินค้า/อุปกรณ์)";
                            item.StatusBadgeClass = "bg-success text-white";
                            break;
                        case "CHECKED_IN":
                            item.StatusText = "เช็คอินเข้าสนามแล้ว";
                            item.StatusBadgeClass = "bg-primary text-white";
                            break;
                        case "COMPLETED":
                            item.StatusText = "การใช้บริการเสร็จสิ้น";
                            item.StatusBadgeClass = "bg-secondary text-white";
                            break;
                        case "CANCELLED":
                            item.StatusText = "ยกเลิกการจอง";
                            item.StatusBadgeClass = "bg-danger text-white";
                            break;
                        default:
                            item.StatusText = "รอชำระเงิน";
                            item.StatusBadgeClass = "bg-info text-dark";
                            break;
                    }

                    foreach (var s in b.BookingSlots)
                    {
                        var courtName = s.Court?.CourtName ?? $"Court {s.CourtId}";
                        var timeRange = s.Slot != null ? $"{s.Slot.StartTime:HH:mm} - {s.Slot.EndTime:HH:mm}" : $"Slot #{s.SlotId}";
                        item.CourtSlots.Add($"{courtName} ({timeRange})");
                    }

                    foreach (var itm in b.BookingItems)
                    {
                        var pName = itm.Product?.ProductName ?? $"สินค้า #{itm.ProductId}";
                        var kind = itm.ItemType == "RENT" ? "[เช่า]" : "[ซื้อ]";
                        item.ItemsSummary.Add($"{kind} {pName} x {itm.Quantity}");
                    }

                    myBookingsList.Add(item);
                }
            }
            // ถ้าเป็น Guest -> myBookingsList ว่างเปล่า ไม่ดึงข้อมูลการจองของคนอื่น
            ViewBag.MyBookings = myBookingsList;
            ViewBag.IsUserLoggedIn = isUserLoggedIn;
        }
        catch
        {
            ViewBag.Courts = new List<Court>();
            ViewBag.TimeSlots = new List<TimeSlot>();
            ViewBag.Promotions = new List<Promotion>();
            ViewBag.Products = new List<Product>();
            ViewBag.MyBookings = new List<BookingTrackItem>();
        }

        return View();
    }

    // ============================================================
    // CUSTOMER: บันทึกการจองคอร์ท + สั่งซื้อ/เช่าอุปกรณ์ + อัปโหลดสลิป
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitBooking(
        string customerName,
        string customerPhone,
        string playDate,
        string selectedSlotsJson,
        string cartItemsJson,
        string? promoCode,
        IFormFile? slipFile)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(customerName) || string.IsNullOrWhiteSpace(customerPhone))
            {
                return Json(new { success = false, message = "กรุณาระบุชื่อ-นามสกุล และเบอร์โทรศัพท์" });
            }

            if (!DateOnly.TryParseExact(playDate?.Trim(), "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out DateOnly targetPlayDate))
            {
                return Json(new { success = false, message = "วันที่เล่นไม่ถูกต้อง กรุณาเลือกวันที่ใหม่" });
            }
            if (targetPlayDate < DateOnly.FromDateTime(DateTime.Today))
                return Json(new { success = false, message = "วันที่เลือกผ่านไปแล้ว ไม่สามารถจองย้อนหลังได้ กรุณาเลือกวันนี้หรือวันถัดไป" });
            if (targetPlayDate > DateOnly.FromDateTime(DateTime.Today).AddDays(7))
                return Json(new { success = false, message = "จองล่วงหน้าได้ไม่เกิน 7 วัน กรุณาเลือกวันที่ภายในช่วงที่กำหนด" });

            // 1. ตรวจสอบสิทธิ์ผู้ใช้งาน (Guest ไม่สามารถจองหรือชำระเงินได้ ต้องเข้าสู่ระบบด้วยบัญชี Customer ก่อน)
            string? loggedInUidStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(loggedInUidStr) || !uint.TryParse(loggedInUidStr, out uint lUid))
            {
                return Json(new { success = false, requireLogin = true, message = "ผู้เยี่ยมชม (Guest) ไม่สามารถทำการจองหรือชำระเงินได้ กรุณาเข้าสู่ระบบด้วยบัญชีสมาชิกก่อนทำรายการ" });
            }

            User? user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == lUid);
            if (user == null || user.Role?.RoleCode != "CUSTOMER" || (user.Status ?? "").Equals("inactive", StringComparison.OrdinalIgnoreCase))
            {
                return Json(new { success = false, requireLogin = true, message = "บัญชีนี้ไม่มีสิทธิ์จอง กรุณาเข้าสู่ระบบด้วยบัญชีสมาชิก" });
            }

            // 2. Parse รายการช่วงเวลาของสนามที่เลือก
            var slotsList = new List<SlotSelectionInput>();
            if (!string.IsNullOrWhiteSpace(selectedSlotsJson))
            {
                slotsList = JsonSerializer.Deserialize<List<SlotSelectionInput>>(selectedSlotsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
            }

            // 3. Parse รายการสินค้า/อุปกรณ์ที่สั่งซื้อหรือเช่า
            var cartList = new List<CartItemInput>();
            if (!string.IsNullOrWhiteSpace(cartItemsJson))
            {
                cartList = JsonSerializer.Deserialize<List<CartItemInput>>(cartItemsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
            }

            if (!slotsList.Any() && !cartList.Any())
            {
                return Json(new { success = false, message = "กรุณาเลือกช่วงเวลาคอร์ท หรือสินค้า/อุปกรณ์อย่างน้อย 1 รายการ" });
            }

            if (slotsList.Any(s => s.CourtId <= 0 || s.CourtId > ushort.MaxValue || s.SlotId <= 0 || s.SlotId > byte.MaxValue) ||
                slotsList.GroupBy(s => $"{s.CourtId}_{s.SlotId}").Any(g => g.Count() > 1) ||
                cartList.Any(i => i.ProductId <= 0 || i.Qty <= 0 || (i.Type != "SALE" && i.Type != "RENT")))
            {
                return Json(new { success = false, message = "ข้อมูลคอร์ทหรือสินค้าไม่ถูกต้อง กรุณาเลือกใหม่" });
            }

            // Prices and item metadata must come from the database, never from browser payloads.
            var selectedCourtIds = slotsList.Select(s => (ushort)s.CourtId).Distinct().ToList();
            var selectedSlotIds = slotsList.Select(s => (byte)s.SlotId).Distinct().ToList();
            var courtsById = await _db.Courts
                .Where(c => selectedCourtIds.Contains(c.CourtId) && c.Status == "AVAILABLE")
                .ToDictionaryAsync(c => c.CourtId);
            var slotsById = await _db.TimeSlots
                .Where(s => selectedSlotIds.Contains(s.SlotId) && (s.IsActive == true || s.IsActive == null))
                .ToDictionaryAsync(s => s.SlotId);
            if (slotsList.Any(s => !courtsById.ContainsKey((ushort)s.CourtId) || !slotsById.ContainsKey((byte)s.SlotId)))
            {
                return Json(new { success = false, message = "คอร์ทหรือช่วงเวลาที่เลือกไม่พร้อมให้จองแล้ว" });
            }

            var productIds = cartList.Select(i => (uint)i.ProductId).Distinct().ToList();
            var productsById = await _db.Products.Include(p => p.Category)
                .Where(p => productIds.Contains(p.ProductId) && (p.IsActive == true || p.IsActive == null))
                .ToDictionaryAsync(p => p.ProductId);
            if (cartList.Any(i => !productsById.ContainsKey((uint)i.ProductId)))
            {
                return Json(new { success = false, message = "มีสินค้าที่ปิดขายหรือไม่พบในระบบ กรุณาตรวจสอบตะกร้า" });
            }

            foreach (var item in cartList)
            {
                var product = productsById[(uint)item.ProductId];
                if (!string.Equals(item.Type, product.ProductType, StringComparison.OrdinalIgnoreCase))
                    return Json(new { success = false, message = "ประเภทสินค้าในตะกร้าไม่ตรงกับข้อมูลระบบ กรุณาโหลดหน้าใหม่" });
                item.Price = product.Price;
                item.ProductName = product.ProductName;
                item.CategoryCode = product.Category?.CategoryCode ?? string.Empty;
            }

            foreach (var selected in slotsList)
            {
                var court = courtsById[(ushort)selected.CourtId];
                var timeSlot = slotsById[(byte)selected.SlotId];
                var durationHours = (decimal)(timeSlot.EndTime.ToTimeSpan() - timeSlot.StartTime.ToTimeSpan()).TotalMinutes / 60m;
                if (durationHours <= 0)
                    return Json(new { success = false, message = "ช่วงเวลาคอร์ทไม่ถูกต้อง" });
                selected.Price = Math.Round(court.PricePerHour * durationHours, 2);
            }

            await using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            var occupiedSlots = await _db.BookingSlots
                .Where(bs => bs.PlayDate == targetPlayDate && bs.LockKey == 1 &&
                    selectedCourtIds.Contains(bs.CourtId) && selectedSlotIds.Contains(bs.SlotId) &&
                    bs.Booking.Status != "CANCELLED" && bs.Booking.Status != "REJECTED" && bs.Booking.Status != "EXPIRED")
                .Select(bs => new { bs.CourtId, bs.SlotId })
                .ToListAsync();
            if (slotsList.Any(s => occupiedSlots.Any(o => o.CourtId == s.CourtId && o.SlotId == s.SlotId)))
                return Json(new { success = false, message = "ช่วงเวลาบางรายการถูกจองไปแล้ว กรุณาเลือกช่วงเวลาใหม่" });

            var playDayStart = targetPlayDate.ToDateTime(TimeOnly.MinValue);
            var playDayEnd = targetPlayDate.AddDays(1).ToDateTime(TimeOnly.MinValue);
            var courtBlocks = await _db.CourtBlocks
                .Where(b => selectedCourtIds.Contains(b.CourtId) && b.BlockStart < playDayEnd && b.BlockEnd > playDayStart)
                .ToListAsync();
            bool selectedBlockedSlot = slotsList.Any(selection =>
            {
                var slot = slotsById[(byte)selection.SlotId];
                var slotStart = targetPlayDate.ToDateTime(slot.StartTime);
                var slotEnd = targetPlayDate.ToDateTime(slot.EndTime);
                return courtBlocks.Any(block => block.CourtId == selection.CourtId &&
                    block.BlockStart < slotEnd && block.BlockEnd > slotStart);
            });
            if (selectedBlockedSlot)
                return Json(new { success = false, message = "ช่วงเวลาที่เลือกถูกล็อกเพื่อซ่อมบำรุง กรุณาเลือกเวลาอื่น" });

            var walkInSlots = await _db.WalkInQueues
            .Where(q => q.QueueDate == targetPlayDate && q.Status != "CANCELLED" &&
                    q.CourtId.HasValue && q.SlotId.HasValue && selectedCourtIds.Contains(q.CourtId.Value) &&
                    selectedSlotIds.Contains(q.SlotId.Value))
                .Select(q => new { CourtId = q.CourtId!.Value, SlotId = q.SlotId!.Value })
                .ToListAsync();
            if (slotsList.Any(s => walkInSlots.Any(w => w.CourtId == s.CourtId && w.SlotId == s.SlotId)))
                return Json(new { success = false, message = "ช่วงเวลานี้ถูกจัดให้คิว Walk-in แล้ว กรุณาเลือกเวลาอื่น" });

            foreach (var grouped in cartList.Where(i => i.Type == "SALE").GroupBy(i => (uint)i.ProductId))
            {
                if (productsById[grouped.Key].StockQty < grouped.Sum(i => i.Qty))
                    return Json(new { success = false, message = $"สินค้า {productsById[grouped.Key].ProductName} มีจำนวนไม่เพียงพอ" });
            }
            foreach (var grouped in cartList.Where(i => i.Type == "RENT").GroupBy(i => (uint)i.ProductId))
            {
                var reservedQty = await _db.BookingItems
                    .Where(i => i.ProductId == grouped.Key && i.ItemType == "RENT" && i.Booking.PlayDate == targetPlayDate &&
                        (i.Booking.Status == "PENDING_PAYMENT" || i.Booking.Status == "PENDING_VERIFY" || i.Booking.Status == "CONFIRMED"))
                    .SumAsync(i => (int?)i.Quantity) ?? 0;
                if (productsById[grouped.Key].StockQty - reservedQty < grouped.Sum(i => i.Qty))
                    return Json(new { success = false, message = $"อุปกรณ์ {productsById[grouped.Key].ProductName} มีจำนวนให้เช่าไม่เพียงพอในวันที่เลือก" });
            }

            // 4. คำนวณราคาและส่วนลดโปรโมชั่นอัตโนมัติ
            decimal courtAmount = slotsList.Sum(s => s.Price);
            decimal saleAmount = 0m;
            decimal rentalAmount = 0m;
            decimal totalDiscounts = 0m;
            var appliedPromoNames = new List<string>();

            // แยกยอดซื้อและเช่า
            foreach (var item in cartList)
            {
                decimal itemSubtotal = item.Price * item.Qty;
                if (item.Type == "RENT") rentalAmount += itemSubtotal;
                else saleAmount += itemSubtotal;
            }

            var activePromotions = await _db.Promotions.Include(p => p.PromotionRules)
                .Where(p => p.IsActive == true && (p.StartDate == null || p.StartDate <= targetPlayDate) && (p.EndDate == null || p.EndDate >= targetPlayDate))
                .OrderByDescending(p => p.UpdatedAt)
                .ThenByDescending(p => p.PromotionId)
                .ToListAsync();
            Promotion? GetPromotion(string type) => activePromotions.FirstOrDefault(p => p.PromoType == type);
            var appliedPromotionAmounts = new List<(Promotion Promotion, decimal Amount)>();
            bool CanApplyPromotion(Promotion promotion) =>
                !appliedPromotionAmounts.Any(x => x.Promotion.IsStackable == false) &&
                (promotion.IsStackable != false || appliedPromotionAmounts.Count == 0);

            // --- โปรโมชั่นที่ 1: Time-based Court Discount ---
            var timePromotion = GetPromotion("TIME_BASED");
            var timePromoDays = (timePromotion?.ActiveDays ?? "1,2,3,4,5")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(value => int.TryParse(value, out var day) ? day : -1)
                .Where(day => day >= 0 && day <= 6)
                .ToHashSet();
            bool isPromoDay = timePromoDays.Contains((int)targetPlayDate.DayOfWeek);
            decimal courtDiscount = 0m;
            if (timePromotion != null && CanApplyPromotion(timePromotion) && isPromoDay && timePromotion.TimeStart.HasValue && timePromotion.TimeEnd.HasValue)
            {
                foreach (var s in slotsList)
                {
                    var selectedTime = slotsById[(byte)s.SlotId];
                    if (selectedTime.StartTime >= timePromotion.TimeStart.Value && selectedTime.EndTime <= timePromotion.TimeEnd.Value)
                    {
                        courtDiscount += Math.Round(s.Price * timePromotion.DiscountValue / 100m, 2);
                    }
                }
                if (courtDiscount > 0)
                {
                    totalDiscounts += courtDiscount;
                    appliedPromotionAmounts.Add((timePromotion, courtDiscount));
                    appliedPromoNames.Add($"{timePromotion.PromoName} (ลด {timePromotion.DiscountValue:N0}% รวม {courtDiscount:N2} ฿)");
                }
            }

            // --- โปรโมชั่นที่ 2: Volume-based Shuttlecock Sale (ซื้อลูกแบดตั้งแต่ 2 หลอดขึ้นไป หลอดที่ 2 ลด 50%) ---
            var volumePromotion = GetPromotion("VOLUME_SALE");
            var volumeCondition = volumePromotion?.PromotionRules.FirstOrDefault(r => r.RuleKind == "CONDITION");
            var volumeReward = volumePromotion?.PromotionRules.FirstOrDefault(r => r.RuleKind == "REWARD");
            var shuttleItems = cartList.Where(i => i.Type == "SALE" && (volumeCondition?.CategoryId == null || productsById[(uint)i.ProductId].CategoryId == volumeCondition.CategoryId)).ToList();
            int totalShuttleQty = shuttleItems.Sum(i => i.Qty);
            int volumeRequiredQty = (int)(volumeCondition?.RequiredQty ?? 2);
            int volumeNthItem = (int)(volumeReward?.NthItem ?? 2);
            if (volumePromotion != null && CanApplyPromotion(volumePromotion) && totalShuttleQty >= volumeRequiredQty && volumeNthItem > 0)
            {
                var unitPrices = shuttleItems.SelectMany(i => Enumerable.Repeat(i.Price, i.Qty)).OrderBy(p => p).ToList();
                decimal shuttleDiscount = unitPrices.Count >= volumeNthItem
                    ? Math.Round(unitPrices[volumeNthItem - 1] * volumePromotion.DiscountValue / 100m, 2) : 0m;
                totalDiscounts += shuttleDiscount;
                if (shuttleDiscount > 0)
                {
                    appliedPromotionAmounts.Add((volumePromotion, shuttleDiscount));
                    appliedPromoNames.Add($"{volumePromotion.PromoName} (ลด {volumePromotion.DiscountValue:N0}% รวม {shuttleDiscount:N2} ฿)");
                }
            }

            // --- โปรโมชั่นที่ 3: Bundle Rental ---
            var bundlePromotion = GetPromotion("BUNDLE_RENTAL");
            var bundleConditions = bundlePromotion?.PromotionRules.Where(r => r.RuleKind == "CONDITION" && r.CategoryId.HasValue).ToList() ?? new List<PromotionRule>();
            bool bundleEligible = bundlePromotion != null && CanApplyPromotion(bundlePromotion) && bundleConditions.Count >= 2 && bundleConditions.All(rule =>
                cartList.Where(i => i.Type == "RENT" && productsById[(uint)i.ProductId].CategoryId == rule.CategoryId).Sum(i => i.Qty) >= rule.RequiredQty);
            if (bundleEligible)
            {
                var bundleCategoryIds = bundleConditions.Select(r => r.CategoryId!.Value).ToHashSet();
                decimal equipmentRentalSubtotal = cartList.Where(i => i.Type == "RENT" && bundleCategoryIds.Contains(productsById[(uint)i.ProductId].CategoryId)).Sum(i => i.Price * i.Qty);
                decimal bundleDiscount = Math.Round(equipmentRentalSubtotal * bundlePromotion!.DiscountValue / 100m, 2);
                totalDiscounts += bundleDiscount;
                appliedPromotionAmounts.Add((bundlePromotion, bundleDiscount));
                appliedPromoNames.Add($"{bundlePromotion.PromoName} (ลด {bundlePromotion.DiscountValue:N0}% รวม {bundleDiscount:N2} ฿)");
            }
            // --- โปรโมชั่นที่ 4: Cross-selling (ซื้อลูกแบด 1 หลอด + เช่าไม้ 2 ไม้ -> เช่ารองเท้าฟรี 1 คู่) ---
            var crossPromotion = GetPromotion("CROSS_SELL");
            var crossConditions = crossPromotion?.PromotionRules.Where(r => r.RuleKind == "CONDITION" && r.CategoryId.HasValue).ToList() ?? new List<PromotionRule>();
            var crossReward = crossPromotion?.PromotionRules.FirstOrDefault(r => r.RuleKind == "REWARD" && r.CategoryId.HasValue);
            bool crossEligible = crossPromotion != null && CanApplyPromotion(crossPromotion) && crossReward != null && crossConditions.Count > 0 && crossConditions.All(rule =>
                cartList.Where(i => (rule.TargetType == "SALE_ITEM" ? i.Type == "SALE" : i.Type == "RENT") && productsById[(uint)i.ProductId].CategoryId == rule.CategoryId).Sum(i => i.Qty) >= rule.RequiredQty);
            if (crossEligible)
            {
                var rewardItems = cartList.Where(i => i.Type == "RENT" && productsById[(uint)i.ProductId].CategoryId == crossReward!.CategoryId).ToList();
                int configuredFreeQty = Math.Max(1, (int)crossPromotion!.DiscountValue);
                int freeQty = Math.Min(configuredFreeQty, rewardItems.Sum(i => i.Qty));
                decimal freeItemDiscount = rewardItems.SelectMany(i => Enumerable.Repeat(i.Price, i.Qty)).OrderByDescending(p => p).Take(freeQty).Sum();
                totalDiscounts += freeItemDiscount;
                appliedPromotionAmounts.Add((crossPromotion, freeItemDiscount));
                appliedPromoNames.Add($"{crossPromotion.PromoName} (รับฟรี {freeQty} ชิ้น มูลค่า {freeItemDiscount:N2} ฿)");
            }

            // --- Promotion 5: first successful booking; account registration date is not used. ---
            decimal currentSubtotal = (courtAmount + saleAmount + rentalAmount) - totalDiscounts;
            var totalBillPromotion = GetPromotion("TOTAL_BILL");
            bool isFirstBooking = !await _db.Bookings.AnyAsync(b => b.UserId == user.UserId &&
                (b.Status == "CONFIRMED" || b.Status == "CHECKED_IN" || b.Status == "COMPLETED" ||
                 b.Payments.Any(p => p.Status == "APPROVED" || p.Status == "PAID")));
            decimal minimumBill = totalBillPromotion?.MinBillAmount ?? 600m;
            bool promoCodeMatch = string.IsNullOrWhiteSpace(promoCode) ||
                promoCode.Trim().Equals(totalBillPromotion?.PromoCode, StringComparison.OrdinalIgnoreCase) ||
                promoCode.Trim().Equals("PROMO_TOTAL", StringComparison.OrdinalIgnoreCase) ||
                promoCode.Trim().Equals("NEW_MEMBER_100", StringComparison.OrdinalIgnoreCase);
            if (totalBillPromotion != null && CanApplyPromotion(totalBillPromotion) && slotsList.Count > 0 && currentSubtotal >= minimumBill && promoCodeMatch && (!totalBillPromotion.FirstBookingOnly || isFirstBooking))
            {
                decimal totalBillDiscount = Math.Min(totalBillPromotion.DiscountValue, currentSubtotal);
                totalDiscounts += totalBillDiscount;
                appliedPromotionAmounts.Add((totalBillPromotion, totalBillDiscount));
                appliedPromoNames.Add($"{totalBillPromotion.PromoName} (ลด {totalBillDiscount:N2} ฿)");
            }

            decimal netAmount = Math.Max(0m, (courtAmount + saleAmount + rentalAmount) - totalDiscounts);

            // 5. จัดการอัปโหลดสลิปชำระเงิน
            string? slipUrl = null;
            if (slipFile != null && slipFile.Length > 0)
            {
                var extension = await GetSlipImageExtensionAsync(slipFile);
                if (extension == null)
                    return Json(new { success = false, message = "ไฟล์สลิปต้องเป็นภาพ JPG, PNG หรือ WEBP ขนาดไม่เกิน 5 MB" });

                var uploadsFolder = UploadStorage.GetSlipDirectory(_env);
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                var safeFileName = $"slip_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..12]}{extension}";
                var filePath = Path.Combine(uploadsFolder, safeFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await slipFile.CopyToAsync(stream);
                }
                slipUrl = $"/uploads/slips/{safeFileName}";
            }

            foreach (var grouped in cartList.Where(i => i.Type == "SALE").GroupBy(i => (uint)i.ProductId))
            {
                var quantity = grouped.Sum(i => i.Qty);
                var changedRows = await _db.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE products SET stock_qty = stock_qty - {quantity}, updated_at = CURRENT_TIMESTAMP WHERE product_id = {grouped.Key} AND stock_qty >= {quantity} AND (is_active = 1 OR is_active IS NULL)");
                if (changedRows != 1)
                    return Json(new { success = false, message = $"สินค้า {productsById[grouped.Key].ProductName} มีจำนวนไม่เพียงพอแล้ว กรุณาตรวจตะกร้าอีกครั้ง" });
            }

            // 6. บันทึก Booking Header ลงตาราง bookings
            string randomSuffix = new Random().Next(1000, 9999).ToString();
            string bookingCode = $"BK-{targetPlayDate:yyyyMMdd}-{randomSuffix}";

            var booking = new Booking
            {
                BookingCode = bookingCode,
                UserId = user.UserId,
                PlayDate = targetPlayDate,
                IsFirstBooking = isFirstBooking,
                CourtAmount = courtAmount,
                SaleAmount = saleAmount,
                RentalAmount = rentalAmount,
                DiscountAmount = totalDiscounts,
                NetAmount = netAmount,
                Status = !string.IsNullOrEmpty(slipUrl) ? "PENDING_VERIFY" : "PENDING_PAYMENT",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            _db.Bookings.Add(booking);
            await _db.SaveChangesAsync();

            foreach (var (promotion, amount) in appliedPromotionAmounts.Where(x => x.Amount > 0))
            {
                _db.BookingPromotions.Add(new BookingPromotion
                {
                    BookingId = booking.BookingId,
                    PromotionId = promotion.PromotionId,
                    DiscountAmount = amount,
                    Detail = promotion.PromoName
                });
            }

            // 7. บันทึก BookingSlots
            foreach (var s in slotsList)
            {
                var bSlot = new BookingSlot
                {
                    BookingId = booking.BookingId,
                    CourtId = (ushort)s.CourtId,
                    SlotId = (byte)s.SlotId,
                    PlayDate = targetPlayDate,
                    Price = s.Price,
                    DiscountAmount = 0m,
                    LockKey = 1
                };
                _db.BookingSlots.Add(bSlot);
            }

            // 8. บันทึก BookingItems และตัดสต็อกสินค้าขาย
            foreach (var itm in cartList)
            {
                var bItem = new BookingItem
                {
                    BookingId = booking.BookingId,
                    ProductId = (uint)itm.ProductId,
                    ItemType = itm.Type,
                    Quantity = (uint)itm.Qty,
                    UnitPrice = itm.Price,
                    DiscountAmount = 0m,
                    LineTotal = itm.Price * itm.Qty,
                    ItemStatus = "RESERVED"
                };
                _db.BookingItems.Add(bItem);

            }

            // 9. บันทึก Payment
            if (!string.IsNullOrEmpty(slipUrl))
            {
                var payment = new Payment
                {
                    BookingId = booking.BookingId,
                    Amount = netAmount,
                    TransferAt = DateTime.Now,
                    SlipImageUrl = slipUrl,
                    Status = "PENDING"
                };
                _db.Payments.Add(payment);
            }

            // อัปเดตสถานะ HasBooked ของผู้ใช้
            if (slotsList.Count > 0) user.HasBooked = true;
            user.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return Json(new
            {
                success = true,
                bookingCode = booking.BookingCode,
                playDate = targetPlayDate.ToString("dd/MM/yyyy"),
                netAmount = netAmount,
                discountAmount = totalDiscounts,
                appliedPromos = appliedPromoNames,
                status = booking.Status,
                statusText = !string.IsNullOrEmpty(slipUrl) ? "รอฝ่ายบัญชีตรวจสอบสลิป" : "รอชำระเงิน",
                slipUrl = string.IsNullOrEmpty(slipUrl) ? null : Url.Action(nameof(ViewSlip), new { bookingCode = booking.BookingCode }),
                message = "บันทึกการจองและคำนวณส่วนลดโปรโมชั่นสำเร็จเรียบร้อย!"
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "เกิดข้อผิดพลาดในการบันทึกการจอง: " + ex.Message });
        }
    }

    // ============================================================
    // CUSTOMER: ติดตามสถานะการจองแบบ Real-time (Track Booking)
    // ============================================================
    [HttpGet]
    public async Task<IActionResult> TrackBooking(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Json(new { success = false, message = "กรุณาระบุรหัสการจอง หรือเบอร์โทรศัพท์" });
        }

        var searchStr = code.Trim().ToUpper();
        var b = await _db.Bookings
            .Include(x => x.BookingSlots).ThenInclude(bs => bs.Court)
            .Include(x => x.BookingSlots).ThenInclude(bs => bs.Slot)
            .Include(x => x.BookingItems).ThenInclude(bi => bi.Product)
            .Include(x => x.Payments)
            .Include(x => x.User)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(x => x.BookingCode.ToUpper() == searchStr || (x.User != null && x.User.Phone == code.Trim()));

        if (b == null)
        {
            return Json(new { success = false, message = $"ไม่พบข้อมูลการจองสำหรับ '{code}'" });
        }

        bool isOwner = uint.TryParse(HttpContext.Session.GetString("UserId"), out var trackingUserId)
            && b.UserId == trackingUserId;
        bool searchedByBookingCode = string.Equals(b.BookingCode, searchStr, StringComparison.OrdinalIgnoreCase);
        if (!isOwner && !searchedByBookingCode)
            return Json(new { success = false, requireLogin = true, message = "ค้นหาด้วยเบอร์โทรศัพท์ได้เมื่อเข้าสู่ระบบบัญชีเจ้าของรายการเท่านั้น กรุณาใช้รหัสการจองหรือเข้าสู่ระบบ" });

        int step = 2; // 1: จองแล้ว, 2: รอชำระ, 3: รอตรวจสอบ, 4: สำเร็จ
        bool hasCourtBooking = b.BookingSlots.Any();
        string statusText = "รอชำระเงิน";
        string badgeClass = "bg-warning text-dark";

        switch (b.Status?.ToUpper())
        {
            case "PENDING_VERIFY":
                step = 3;
                statusText = "รอฝ่ายบัญชีตรวจสอบสลิป";
                badgeClass = "bg-warning text-dark";
                break;
            case "CONFIRMED":
                step = 4;
                statusText = hasCourtBooking
                    ? "อนุมัติการชำระเงินแล้ว (ยืนยันคิวสนามเรียบร้อย)"
                    : "อนุมัติการชำระเงินแล้ว (รอรับสินค้า/อุปกรณ์)";
                badgeClass = "bg-success text-white";
                break;
            case "CHECKED_IN":
                step = 4;
                statusText = "เช็คอินเข้าใช้งานสนามแล้ว";
                badgeClass = "bg-primary text-white";
                break;
            case "COMPLETED":
                step = 4;
                statusText = "การใช้บริการเสร็จสิ้นสมบูรณ์";
                badgeClass = "bg-secondary text-white";
                break;
            case "CANCELLED":
                step = -1;
                statusText = "ยกเลิกการจอง";
                badgeClass = "bg-danger text-white";
                break;
            default:
                step = 2;
                statusText = "รอชำระเงิน/แนบสลิป";
                badgeClass = "bg-info text-dark";
                break;
        }

        var latestPayment = b.Payments.OrderByDescending(p => p.PaymentId).FirstOrDefault();

        // ผู้ไม่ใช่เจ้าของเห็นได้เฉพาะสถานะจากรหัสการจอง ไม่ส่งข้อมูลส่วนตัวหรือข้อมูลการชำระเงินกลับไป
        if (!isOwner)
        {
            return Json(new
            {
                success = true,
                isOwner = false,
                bookingCode = b.BookingCode,
                playDate = b.PlayDate.ToString("dd/MM/yyyy"),
                hasCourtBooking,
                statusText,
                badgeClass,
                step,
                createdAt = b.CreatedAt.ToString("dd/MM/yyyy HH:mm")
            });
        }

        string displayName = b.User?.FullName ?? "คุณลูกค้า";
        string displayPhone = b.User?.Phone ?? "-";
        return Json(new
        {
            success = true,
            bookingCode = b.BookingCode,
            playDate = b.PlayDate.ToString("dd/MM/yyyy"),
            customerName = displayName,
            customerPhone = displayPhone,
            courtAmount = b.CourtAmount,
            saleAmount = b.SaleAmount,
            rentalAmount = b.RentalAmount,
            discountAmount = b.DiscountAmount,
            netAmount = b.NetAmount,
            status = b.Status,
            hasCourtBooking = hasCourtBooking,
            statusText = statusText,
            badgeClass = badgeClass,
            step = step,
            slipUrl = latestPayment == null || string.IsNullOrEmpty(latestPayment.SlipImageUrl)
                ? null : Url.Action(nameof(ViewSlip), new { bookingCode = b.BookingCode }),
            createdAt = b.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
            isOwner = isOwner,
            courts = b.BookingSlots.Select(s => new {
                courtName = s.Court?.CourtName ?? $"Court {s.CourtId}",
                timeRange = s.Slot != null ? $"{s.Slot.StartTime:HH:mm} - {s.Slot.EndTime:HH:mm}" : $"Slot #{s.SlotId}",
                price = s.Price
            }),
            items = b.BookingItems.Select(i => new {
                name = i.Product?.ProductName ?? $"สินค้า #{i.ProductId}",
                type = i.ItemType,
                qty = i.Quantity,
                unitPrice = i.UnitPrice,
                lineTotal = i.LineTotal
            })
        });
    }

    [HttpGet]
    public async Task<IActionResult> ViewSlip(string bookingCode)
    {
        if (!uint.TryParse(HttpContext.Session.GetString("UserId"), out var currentUserId))
            return Unauthorized();

        var booking = await _db.Bookings.Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.BookingCode == bookingCode);
        if (booking == null) return NotFound();

        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == currentUserId);
        bool isOwner = booking.UserId == currentUserId;
        bool isAuthorizedStaff = user?.Role?.RoleCode == "ACCOUNTING";
        if (!isOwner && !isAuthorizedStaff) return Forbid();

        var slipPath = booking.Payments.OrderByDescending(p => p.PaymentId)
            .Select(p => p.SlipImageUrl).FirstOrDefault(path => !string.IsNullOrWhiteSpace(path));
        if (string.IsNullOrWhiteSpace(slipPath)) return NotFound();

        var extension = Path.GetExtension(slipPath).ToLowerInvariant();
        var contentType = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => null
        };
        if (contentType == null) return NotFound();

        var fileName = Path.GetFileName(slipPath);
        var fullPath = UploadStorage.GetSlipPath(_env, slipPath);
        if (!System.IO.File.Exists(fullPath)) return NotFound();

        Response.Headers.CacheControl = "no-store, private";
        return PhysicalFile(fullPath, contentType);
    }

    // ============================================================
    // CUSTOMER: อัปโหลดสลิปเพิ่มเติมสำหรับรายการจองที่มีอยู่แล้ว
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadBookingSlip(string bookingCode, IFormFile slipFile)
    {
        if (!uint.TryParse(HttpContext.Session.GetString("UserId"), out var currentUserId))
        {
            return Json(new { success = false, requireLogin = true, message = "กรุณาเข้าสู่ระบบก่อนแนบสลิป" });
        }

        if (slipFile == null || slipFile.Length == 0)
        {
            return Json(new { success = false, message = "กรุณาเลือกไฟล์สลิปโอนเงิน" });
        }

        var extension = await GetSlipImageExtensionAsync(slipFile);
        if (extension == null)
        {
            return Json(new { success = false, message = "รองรับไฟล์ภาพ JPG, PNG หรือ WEBP ขนาดไม่เกิน 5 MB" });
        }

        var b = await _db.Bookings
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.BookingCode == bookingCode);

        if (b == null)
        {
            return Json(new { success = false, message = "ไม่พบรายการจองนี้ในระบบ" });
        }

        if (b.UserId != currentUserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var uploadsFolder = UploadStorage.GetSlipDirectory(_env);
        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
        var safeName = $"slip_{b.BookingCode}_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..12]}{extension}";
        var path = Path.Combine(uploadsFolder, safeName);
        using (var stream = new FileStream(path, FileMode.Create))
        {
            await slipFile.CopyToAsync(stream);
        }
        var slipUrl = $"/uploads/slips/{safeName}";

        var payment = b.Payments.OrderByDescending(p => p.PaymentId).FirstOrDefault();
        if (payment != null)
        {
            payment.SlipImageUrl = slipUrl;
            payment.Amount = b.NetAmount;
            payment.TransferAt = DateTime.Now;
            payment.Status = "PENDING";
        }
        else
        {
            payment = new Payment
            {
                BookingId = b.BookingId,
                Amount = b.NetAmount,
                TransferAt = DateTime.Now,
                SlipImageUrl = slipUrl,
                Status = "PENDING"
            };
            _db.Payments.Add(payment);
        }

        b.Status = "PENDING_VERIFY";
        b.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();

        return Json(new { success = true, message = "อัปโหลดสลิปเรียบร้อย! ส่งเรื่องให้ฝ่ายบัญชีตรวจสอบแล้ว", slipUrl = Url.Action(nameof(ViewSlip), new { bookingCode = b.BookingCode }) });
    }

    private static async Task<string?> GetSlipImageExtensionAsync(IFormFile file)
    {
        if (file.Length <= 0 || file.Length > 5 * 1024 * 1024) return null;

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
    // CUSTOMER: ดึงรายการคอร์ท/ช่วงเวลาที่ถูกจองตามวันที่ (Real-time DB)
    // ============================================================
    [HttpGet]
    public async Task<IActionResult> GetBookedSlots(string date)
    {
        if (!DateOnly.TryParseExact(date?.Trim(), "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out DateOnly targetDate) ||
            targetDate < DateOnly.FromDateTime(DateTime.Today))
        {
            return Json(new { success = false, bookedSlots = new List<string>() });
        }

        var booked = await _db.BookingSlots
                              .Include(bs => bs.Booking)
                              .Where(bs => bs.PlayDate == targetDate && (bs.LockKey == 1 || (bs.Booking != null && bs.Booking.Status != "CANCELLED")))
                              .Select(bs => $"{bs.CourtId}_{bs.SlotId}")
                              .Distinct()
                              .ToListAsync();
        var walkInBooked = await _db.WalkInQueues
            .Where(q => q.QueueDate == targetDate && q.Status != "CANCELLED" &&
                q.CourtId.HasValue && q.SlotId.HasValue)
            .Select(q => $"{q.CourtId}_{q.SlotId}")
            .ToListAsync();
        booked.AddRange(walkInBooked);

        var dayStart = targetDate.ToDateTime(TimeOnly.MinValue);
        var dayEnd = targetDate.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var closedCourtIds = await _db.Courts
            .Where(c => c.Status != "AVAILABLE")
            .Select(c => c.CourtId)
            .ToListAsync();
        if (closedCourtIds.Count > 0)
        {
            var activeSlotIds = await _db.TimeSlots
                .Where(s => s.IsActive == true || s.IsActive == null)
                .Select(s => s.SlotId)
                .ToListAsync();
            foreach (var courtId in closedCourtIds)
            foreach (var slotId in activeSlotIds)
                booked.Add($"{courtId}_{slotId}");
        }

        var activeBlocks = await _db.CourtBlocks
            .Where(b => b.BlockStart < dayEnd && b.BlockEnd > dayStart)
            .ToListAsync();
        if (activeBlocks.Count > 0)
        {
            var activeSlots = await _db.TimeSlots
                .Where(s => s.IsActive == true || s.IsActive == null)
                .ToListAsync();
            foreach (var block in activeBlocks)
            foreach (var slot in activeSlots)
            {
                var slotStart = targetDate.ToDateTime(slot.StartTime);
                var slotEnd = targetDate.ToDateTime(slot.EndTime);
                if (block.BlockStart < slotEnd && block.BlockEnd > slotStart)
                    booked.Add($"{block.CourtId}_{slot.SlotId}");
            }
        }

        var rentalProducts = await _db.Products
            .Where(p => p.ProductType == "RENT" && (p.IsActive == true || p.IsActive == null))
            .Select(p => new { p.ProductId, p.StockQty })
            .ToListAsync();
        var rentalProductIds = rentalProducts.Select(p => p.ProductId).ToList();
        var reservedRentalQuantities = await _db.BookingItems
            .Where(i => rentalProductIds.Contains(i.ProductId) && i.ItemType == "RENT" && i.Booking.PlayDate == targetDate &&
                (i.Booking.Status == "PENDING_PAYMENT" || i.Booking.Status == "PENDING_VERIFY" || i.Booking.Status == "CONFIRMED"))
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(i => (int)i.Quantity) })
            .ToDictionaryAsync(g => g.ProductId, g => g.Quantity);
        var rentalAvailability = rentalProducts.ToDictionary(
            p => p.ProductId,
            p => Math.Max(0, p.StockQty - reservedRentalQuantities.GetValueOrDefault(p.ProductId)));

        return Json(new { success = true, bookedSlots = booked.Distinct().ToList(), rentalAvailability });
    }

    // Helper classes for input deserialization
    public class SlotSelectionInput
    {
        public int CourtId { get; set; }
        public string CourtName { get; set; } = string.Empty;
        public int SlotId { get; set; }
        public string TimeRange { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public class CartItemInput
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Type { get; set; } = "SALE"; // SALE or RENT
        public string CategoryCode { get; set; } = string.Empty;
        public int Qty { get; set; }
        public decimal Price { get; set; }
    }

    [RequireRole("ADMIN")]
    public async Task<IActionResult> Admin()
    {
        ViewData["Title"] = "แดชบอร์ดภาพรวมระบบ";

        var today = DateTime.Today;
        var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
        var firstDayOfMonthDateOnly = DateOnly.FromDateTime(firstDayOfMonth);

        var vm = new AdminDashboardViewModel();

        // 1. Users
        var allUsers = await _db.Users.Include(u => u.Role).ToListAsync();
        vm.TotalUsers = allUsers.Count;
        vm.ActiveUsers = allUsers.Count(u => u.Status != null && (u.Status == "ACTIVE" || u.Status == "active"));
        vm.NewUsersThisMonth = allUsers.Count(u => u.CreatedAt >= firstDayOfMonth);
        vm.RecentUsers = allUsers.OrderByDescending(u => u.UserId).Take(6).ToList();

        // 2. Courts
        var allCourts = await _db.Courts.Include(c => c.BookingSlots).ToListAsync();
        vm.TotalCourts = allCourts.Count;
        vm.AvailableCourts = allCourts.Count(c => c.Status == "AVAILABLE" || c.Status == "available");

        // 3. Bookings
        var allBookings = await _db.Bookings
            .Include(b => b.User)
            .Include(b => b.Payments)
            .Include(b => b.BookingSlots).ThenInclude(bs => bs.Court)
            .ToListAsync();

        vm.TotalBookings = allBookings.Count;
        vm.BookingsThisMonth = allBookings.Count(b => b.PlayDate >= firstDayOfMonthDateOnly);
        vm.PendingBookings = allBookings.Count(b => b.Status == "PENDING_VERIFY");

        // 4. Revenue
        var confirmedBookings = allBookings
            .Where(b => (b.Status == "CONFIRMED" || b.Status == "CHECKED_IN" || b.Status == "COMPLETED" || b.Status == "IN_USE")
                && b.Payments.Any(p => p.Status == "APPROVED"))
            .ToList();
        var paidWalkIns = await _db.WalkInQueues.Where(q => q.PaymentStatus == "PAID").ToListAsync();
        var paidPosSales = await _db.PosSaleTransactions.Where(s => s.PaymentStatus == "PAID").ToListAsync();

        vm.ApprovedBookingRevenue = confirmedBookings.Sum(b => b.NetAmount);
        vm.PaidWalkInRevenue = paidWalkIns.Sum(q => q.CourtFee);
        vm.PosSalesRevenue = paidPosSales.Sum(s => s.NetTotal);
        vm.TotalRevenue = vm.ApprovedBookingRevenue + vm.PaidWalkInRevenue + vm.PosSalesRevenue;
        vm.RevenueThisMonth = confirmedBookings
            .Where(b => (b.ConfirmedAt ?? b.UpdatedAt) >= firstDayOfMonth)
            .Sum(b => b.NetAmount)
            + paidWalkIns.Where(q => q.PaidAt >= firstDayOfMonth).Sum(q => q.CourtFee)
            + paidPosSales.Where(s => s.PaidAt >= firstDayOfMonth).Sum(s => s.NetTotal);

        // 5. Additional Metrics
        vm.ActivePromotions = await _db.Promotions.CountAsync(p => p.IsActive == true);
        var allProducts = await _db.Products.ToListAsync();
        vm.TotalProducts = allProducts.Count;
        vm.LowStockProducts = allProducts.Count(p => p.StockQty <= 5);
        vm.PendingIssues = await _db.CourtIssues.CountAsync(i => i.Status == "OPEN" || i.Status == "IN_PROGRESS");

        // 6. Recent Bookings
        vm.RecentBookings = allBookings.OrderByDescending(b => b.CreatedAt).Take(5).ToList();

        // 7. Court Stats
        var courtColors = new[] { "#2d6a4f", "#1d4ed8", "#d97706", "#7c3aed", "#e11d48", "#059669", "#4f46e5" };
        int colorIdx = 0;
        int maxSlots = allCourts.Any() ? allCourts.Max(c => c.BookingSlots.Count) : 0;
        if (maxSlots == 0) maxSlots = 1;

        foreach (var c in allCourts.OrderBy(c => c.CourtId))
        {
            int slotCount = c.BookingSlots.Count;
            int pct = (int)Math.Round((double)slotCount / maxSlots * 100);
            if (pct > 100) pct = 100;
            if (pct < 10 && slotCount > 0) pct = 15;

            decimal courtRev = c.BookingSlots.Sum(bs => bs.Price);

            vm.CourtStats.Add(new CourtUsageStat
            {
                CourtId = c.CourtId,
                CourtName = c.CourtName,
                TotalSlotsBooked = slotCount,
                Percentage = pct,
                Status = c.Status,
                TotalCourtRevenue = courtRev,
                Color = courtColors[colorIdx % courtColors.Length]
            });
            colorIdx++;
        }

        // 8. Recent Activities Feed
        var activities = new List<AdminActivityItem>();

        foreach (var b in allBookings.OrderByDescending(b => b.CreatedAt).Take(5))
        {
            string courtNames = b.BookingSlots.Any()
                ? string.Join(", ", b.BookingSlots.Select(bs => bs.Court?.CourtName).Where(cn => cn != null).Distinct())
                : "คอร์ทแบดมินตัน";

            activities.Add(new AdminActivityItem
            {
                Title = $"{b.User?.FullName ?? "ลูกค้า"} จอง {courtNames}",
                Description = $"รหัสการจอง #{b.BookingCode} | ยอด {b.NetAmount:N0} ฿ ({b.Status})",
                Timestamp = b.CreatedAt,
                TimeAgo = GetTimeAgo(b.CreatedAt),
                Icon = "bi-calendar-check",
                BadgeColor = b.Status == "CONFIRMED" || b.Status == "COMPLETED" ? "success" : "warning"
            });
        }

        foreach (var u in allUsers.OrderByDescending(u => u.CreatedAt).Take(4))
        {
            activities.Add(new AdminActivityItem
            {
                Title = $"สมาชิกใหม่: {u.FullName}",
                Description = $"{u.Email} ({u.Role?.RoleName ?? "CUSTOMER"})",
                Timestamp = u.CreatedAt,
                TimeAgo = GetTimeAgo(u.CreatedAt),
                Icon = "bi-person-plus",
                BadgeColor = "primary"
            });
        }

        var latestIssues = await _db.CourtIssues.Include(ci => ci.Court).OrderByDescending(ci => ci.CreatedAt).Take(3).ToListAsync();
        foreach (var ci in latestIssues)
        {
            activities.Add(new AdminActivityItem
            {
                Title = $"แจ้งซ่อม/ชำรุด: {ci.Court?.CourtName ?? "อุปกรณ์"}",
                Description = ci.Description,
                Timestamp = ci.CreatedAt,
                TimeAgo = GetTimeAgo(ci.CreatedAt),
                Icon = "bi-exclamation-triangle",
                BadgeColor = "danger"
            });
        }

        vm.RecentActivities = activities.OrderByDescending(a => a.Timestamp).Take(7).ToList();

        return View(vm);
    }

    private static string GetTimeAgo(DateTime dt)
    {
        var span = DateTime.Now - dt;
        if (span.TotalMinutes < 1) return "เมื่อสักครู่";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} นาทีที่แล้ว";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours} ชั่วโมงที่แล้ว";
        if (span.TotalDays < 30) return $"{(int)span.TotalDays} วันที่แล้ว";
        return dt.ToString("dd/MM/yyyy");
    }

  public IActionResult Lab5(string Name, string LastName)
    {
        var Data = new Lab5ViewModel();
        Data.Name = Name;
        Data.LastName = LastName;
        return View(Data);
    }
    public IActionResult Lab5_2()
    {
        var User = new List<Lab5ViewModel>
        {
            new Lab5ViewModel{Name = "a",LastName = "b"},
            new Lab5ViewModel{Name = "c",LastName = "d"}
        };
        return View(User);
    }

    public IActionResult Lab5_3()
    {
        return View();
    }
    [HttpPost]
    public IActionResult Lab5_3(Lab5ViewModel data)
    {
        string a, b, c;
        a = data.Name;
        b = data.LastName;
        // c = a + " " + b;
        c = data.Name + " " + data.LastName;
        ViewBag.Name = c;
        // return View(data);
        return RedirectToAction("Lab5", "Home", new { Name = data.Name, LastName = data.LastName });
    }

 public IActionResult Lab2()
    {
    char A = 'a';
       string Name, Lastname, Class, Years, Code;

    Name = "Chinnaphat";
    Lastname = "Supsombat";
    Class = "L005";
    Years = "3Years";
    Code = "none";

    ViewBag.Name = Name;
    ViewBag.Lastname = Lastname;
    ViewBag.Class = Class;
    ViewBag.Years = Years;
    ViewBag.Code = Code;
    
    int a,b,c,d,e,f,g,h,i,j,sum;
    a=1;
    b=2;
    c=3;
    d=4;
    e=5;
    f=6;
    g=7;
    h=8;
    i=9;
    j=10;
    
    ViewBag.a = a;
    ViewBag.b = b;
    ViewBag.c = c;
    ViewBag.d = d;
    ViewBag.e = e;
    ViewBag.f = f;
    ViewBag.g = g;
    ViewBag.h = h;
    ViewBag.i = i;
    ViewBag.j = j; 
    
    sum = a+b+c+d+e+f+g+h+i+j ;



    ViewBag.sum =sum;
    if (sum > 80)
    {
        ViewBag.Result = "a";
    }
      if (sum > 76)
    {
        ViewBag.Result = "b+";
    }
      if (sum > 70)
    {
        ViewBag.Result = "b";
    }
      if (sum > 66)
    {
        ViewBag.Result = "c+";
    }
      if (sum > 60)
    {
        ViewBag.Result = "c";
    }
      if (sum > 56)
    {
        ViewBag.Result = "d+";
    }
      if (sum > 50)
    {
        ViewBag.Result = "d";
    }
      if (sum < 49)
    {
        ViewBag.Result = "f";
    }
    return View();
    }

    
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
