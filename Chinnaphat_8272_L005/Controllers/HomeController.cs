using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Chinnaphat_8272_L005.Models;
using Chinnaphat_8272_L005.ViewModels;
using System.ComponentModel.Design;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.IO;

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

    public IActionResult Index()
    {
        return RedirectToAction("Customer", "Home");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public async Task<IActionResult> Customer(string? date, string? section)
    {
        // Set section-specific title
        string sectionLower = section?.ToLower()?.Trim() ?? "";
        ViewBag.Section = sectionLower;
        ViewData["Title"] = sectionLower switch
        {
            "booking"  => "จองสนามแบดมินตัน",
            "product"  => "ซื้ออุปกรณ์ & เช่าอุปกรณ์",
            "tracking" => "ดูสถานะการจอง",
            _          => "หน้าหลัก - ระบบจองและซื้ออุปกรณ์"
        };
        string selectedDate = string.IsNullOrWhiteSpace(date)
            ? DateTime.Today.ToString("yyyy-MM-dd")
            : date.Trim();
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
                                        .Where(p => p.IsActive == true)
                                        .OrderBy(p => p.Priority)
                                        .ToListAsync();
            ViewBag.Promotions = activePromos;

            // ดึงรายการสินค้าและอุปกรณ์สำหรับซื้อ/เช่าจากฐานข้อมูล
            var products = await _db.Products
                                   .Include(p => p.Category)
                                   .Where(p => p.IsActive == true || p.IsActive == null)
                                   .OrderBy(p => p.ProductType)
                                   .ThenBy(p => p.CategoryId)
                                   .ToListAsync();
            ViewBag.Products = products;

            if (DateOnly.TryParse(selectedDate, out DateOnly targetDate))
            {
                var booked = await _db.BookingSlots
                                      .Where(bs => bs.PlayDate == targetDate && bs.LockKey == 1)
                                      .Select(bs => new { bs.CourtId, bs.SlotId })
                                      .ToListAsync();
                ViewBag.BookedSlots = booked;
            }

            // ดึงประวัติการจองและติดตามสถานะ (My Bookings)
            var myBookingsList = new List<BookingTrackItem>();
            var rawBookings = await _db.Bookings
                .Include(b => b.BookingSlots).ThenInclude(bs => bs.Court)
                .Include(b => b.BookingSlots).ThenInclude(bs => bs.Slot)
                .Include(b => b.BookingItems).ThenInclude(bi => bi.Product)
                .Include(b => b.Payments)
                .Include(b => b.User)
                .OrderByDescending(b => b.CreatedAt)
                .Take(20)
                .ToListAsync();

            foreach (var b in rawBookings)
            {
                var item = new BookingTrackItem
                {
                    BookingId = b.BookingId,
                    BookingCode = b.BookingCode,
                    PlayDate = b.PlayDate.ToString("yyyy-MM-dd"),
                    CustomerName = b.User?.FullName ?? "คุณลูกค้า",
                    CustomerPhone = b.User?.Phone ?? "-",
                    CourtAmount = b.CourtAmount,
                    SaleAmount = b.SaleAmount,
                    RentalAmount = b.RentalAmount,
                    DiscountAmount = b.DiscountAmount,
                    NetAmount = b.NetAmount,
                    Status = b.Status,
                    CreatedAt = b.CreatedAt,
                    SlipImageUrl = b.Payments.OrderByDescending(p => p.PaymentId).FirstOrDefault()?.SlipImageUrl
                };

                switch (b.Status?.ToUpper())
                {
                    case "PENDING_VERIFICATION":
                        item.StatusText = "รอฝ่ายบัญชีตรวจสอบสลิป";
                        item.StatusBadgeClass = "bg-warning text-dark";
                        break;
                    case "CONFIRMED":
                        item.StatusText = "ยืนยันคิวแล้ว (พร้อมใช้สนาม)";
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
            ViewBag.MyBookings = myBookingsList;
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

            if (!DateOnly.TryParse(playDate, out DateOnly targetPlayDate))
            {
                targetPlayDate = DateOnly.FromDateTime(DateTime.Today);
            }

            // 1. ตรวจสอบสิทธิ์ผู้ใช้งาน (Guest ไม่สามารถจองหรือชำระเงินได้ ต้องเข้าสู่ระบบด้วยบัญชี Customer ก่อน)
            string? loggedInUidStr = HttpContext.Session.GetString("UserId") ?? Request.Cookies["cb_user_id"];
            if (string.IsNullOrEmpty(loggedInUidStr) || !uint.TryParse(loggedInUidStr, out uint lUid))
            {
                return Json(new { success = false, requireLogin = true, message = "ผู้เยี่ยมชม (Guest) ไม่สามารถทำการจองหรือชำระเงินได้ กรุณาเข้าสู่ระบบด้วยบัญชีสมาชิกก่อนทำรายการ" });
            }

            User? user = await _db.Users.FindAsync(lUid);
            if (user == null)
            {
                return Json(new { success = false, requireLogin = true, message = "ไม่พบบัญชีผู้ใช้งานในระบบ กรุณาเข้าสู่ระบบใหม่อีกครั้ง" });
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

            // --- โปรโมชั่นที่ 1: Time-based Court Discount (ช่วง Off-Peak 09:00 - 15:00 วันธรรมดา ลด 20%) ---
            var dayOfWeek = targetPlayDate.DayOfWeek;
            bool isWeekday = dayOfWeek >= DayOfWeek.Monday && dayOfWeek <= DayOfWeek.Friday;
            decimal courtDiscount = 0m;
            if (isWeekday)
            {
                foreach (var s in slotsList)
                {
                    // Slot 2 (09:00) ถึง Slot 7 (14:00-15:00)
                    if (s.SlotId >= 2 && s.SlotId <= 7)
                    {
                        courtDiscount += Math.Round(s.Price * 0.20m, 2);
                    }
                }
                if (courtDiscount > 0)
                {
                    totalDiscounts += courtDiscount;
                    appliedPromoNames.Add($"โปรฯ เช่าสนามช่วง Off-Peak 09:00-15:00 (ลด 20% รวม {courtDiscount:N0} ฿)");
                }
            }

            // --- โปรโมชั่นที่ 2: Volume-based Shuttlecock Sale (ซื้อลูกแบดตั้งแต่ 2 หลอดขึ้นไป หลอดที่ 2 ลด 50%) ---
            var shuttleItems = cartList.Where(i => i.Type == "SALE" && (i.CategoryCode == "SHUTTLECOCK" || i.ProductName.Contains("ลูกแบด"))).ToList();
            int totalShuttleQty = shuttleItems.Sum(i => i.Qty);
            if (totalShuttleQty >= 2)
            {
                var shuttlePrice = shuttleItems.First().Price;
                decimal shuttleDiscount = Math.Round(shuttlePrice * 0.50m, 2);
                totalDiscounts += shuttleDiscount;
                appliedPromoNames.Add($"โปรฯ ซื้อลูกแบดหลอดที่ 2 ลด 50% (ลด {shuttleDiscount:N0} ฿)");
            }

            // --- โปรโมชั่นที่ 3: Bundle Rental (เช่าไม้ 2 ไม้ + รองเท้า 2 คู่ ลดค่าเช่าอุปกรณ์เซ็ตนี้ 20%) ---
            int racketRentalQty = cartList.Where(i => i.Type == "RENT" && (i.CategoryCode == "RACKET" || i.ProductName.Contains("ไม้แบด"))).Sum(i => i.Qty);
            int shoesRentalQty = cartList.Where(i => i.Type == "RENT" && (i.CategoryCode == "SHOES" || i.ProductName.Contains("รองเท้า"))).Sum(i => i.Qty);
            if (racketRentalQty >= 2 && shoesRentalQty >= 2)
            {
                decimal equipmentRentalSubtotal = cartList.Where(i => i.Type == "RENT" && (i.CategoryCode == "RACKET" || i.CategoryCode == "SHOES")).Sum(i => i.Price * i.Qty);
                decimal bundleDiscount = Math.Round(equipmentRentalSubtotal * 0.20m, 2);
                totalDiscounts += bundleDiscount;
                appliedPromoNames.Add($"โปรฯ ส่งเสริมการเช่า Bundle ไม้+รองเท้า (ลด 20% รวม {bundleDiscount:N0} ฿)");
            }
            // --- โปรโมชั่นที่ 4: Cross-selling (ซื้อลูกแบด 1 หลอด + เช่าไม้ 2 ไม้ -> เช่ารองเท้าฟรี 1 คู่) ---
            else if (totalShuttleQty >= 1 && racketRentalQty >= 2 && shoesRentalQty >= 1)
            {
                var shoeItem = cartList.FirstOrDefault(i => i.Type == "RENT" && (i.CategoryCode == "SHOES" || i.ProductName.Contains("รองเท้า")));
                decimal freeShoeDiscount = shoeItem != null ? shoeItem.Price : 50m;
                totalDiscounts += freeShoeDiscount;
                appliedPromoNames.Add($"โปรฯ ซื้อคู่เช่า (แถมสิทธิ์เช่ารองเท้าฟรี 1 คู่ มูลค่า {freeShoeDiscount:N0} ฿)");
            }

            // --- โปรโมชั่นที่ 5: Total Bill Discount (ยอดรวม >= 600 บาท ลดท้ายบิล 100 บาท) ---
            decimal currentSubtotal = (courtAmount + saleAmount + rentalAmount) - totalDiscounts;
            bool isFirstBooking = !user.HasBooked;
            bool promoCodeMatch = !string.IsNullOrWhiteSpace(promoCode) && promoCode.Trim().ToUpper() == "NEW_MEMBER_100";
            if (currentSubtotal >= 600 && (isFirstBooking || promoCodeMatch))
            {
                totalDiscounts += 100m;
                appliedPromoNames.Add("โปรฯ ส่วนลดสมาชิกใหม่/ยอดรวมบิล 600 บาทขึ้นไป (ลด 100 ฿)");
            }

            decimal netAmount = Math.Max(0m, (courtAmount + saleAmount + rentalAmount) - totalDiscounts);

            // 5. จัดการอัปโหลดสลิปชำระเงิน
            string? slipUrl = null;
            if (slipFile != null && slipFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "slips");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                var ext = Path.GetExtension(slipFile.FileName);
                if (string.IsNullOrEmpty(ext)) ext = ".jpg";
                var safeFileName = $"slip_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..6]}{ext}";
                var filePath = Path.Combine(uploadsFolder, safeFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await slipFile.CopyToAsync(stream);
                }
                slipUrl = $"/uploads/slips/{safeFileName}";
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
                Status = !string.IsNullOrEmpty(slipUrl) ? "PENDING_VERIFICATION" : "PENDING_PAYMENT",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            _db.Bookings.Add(booking);
            await _db.SaveChangesAsync();

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

                // ตัดสต็อกสินค้าในกรณีที่เป็นสินค้าขาย (SALE)
                if (itm.Type == "SALE")
                {
                    var prod = await _db.Products.FindAsync((uint)itm.ProductId);
                    if (prod != null)
                    {
                        prod.StockQty = Math.Max(0, prod.StockQty - itm.Qty);
                        prod.UpdatedAt = DateTime.Now;
                    }
                }
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
            user.HasBooked = true;
            user.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();

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
                slipUrl = slipUrl,
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

        int step = 1; // 1: ยื่นจอง, 2: แนบสลิป, 3: บัญชียืนยัน, 4: เช็คอินเข้าสนาม
        string statusText = "รอชำระเงิน";
        string badgeClass = "bg-warning text-dark";

        switch (b.Status?.ToUpper())
        {
            case "PENDING_VERIFICATION":
                step = 2;
                statusText = "รอฝ่ายบัญชีตรวจสอบสลิป";
                badgeClass = "bg-warning text-dark";
                break;
            case "CONFIRMED":
                step = 3;
                statusText = "ฝ่ายบัญชีอนุมัติแล้ว (คิวยืนยันเรียบร้อย)";
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
                step = 1;
                statusText = "รอชำระเงิน/แนบสลิป";
                badgeClass = "bg-info text-dark";
                break;
        }

        var latestPayment = b.Payments.OrderByDescending(p => p.PaymentId).FirstOrDefault();

        return Json(new
        {
            success = true,
            bookingCode = b.BookingCode,
            playDate = b.PlayDate.ToString("dd/MM/yyyy"),
            customerName = b.User?.FullName ?? "คุณลูกค้า",
            customerPhone = b.User?.Phone ?? "-",
            courtAmount = b.CourtAmount,
            saleAmount = b.SaleAmount,
            rentalAmount = b.RentalAmount,
            discountAmount = b.DiscountAmount,
            netAmount = b.NetAmount,
            status = b.Status,
            statusText = statusText,
            badgeClass = badgeClass,
            step = step,
            slipUrl = latestPayment?.SlipImageUrl,
            createdAt = b.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
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

    // ============================================================
    // CUSTOMER: อัปโหลดสลิปเพิ่มเติมสำหรับรายการจองที่มีอยู่แล้ว
    // ============================================================
    [HttpPost]
    public async Task<IActionResult> UploadBookingSlip(string bookingCode, IFormFile slipFile)
    {
        if (slipFile == null || slipFile.Length == 0)
        {
            return Json(new { success = false, message = "กรุณาเลือกไฟล์สลิปโอนเงิน" });
        }

        var b = await _db.Bookings
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.BookingCode == bookingCode);

        if (b == null)
        {
            return Json(new { success = false, message = "ไม่พบรายการจองนี้ในระบบ" });
        }

        var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "slips");
        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
        var ext = Path.GetExtension(slipFile.FileName);
        if (string.IsNullOrEmpty(ext)) ext = ".jpg";
        var safeName = $"slip_{b.BookingCode}_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..4]}{ext}";
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

        b.Status = "PENDING_VERIFICATION";
        b.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();

        return Json(new { success = true, message = "อัปโหลดสลิปเรียบร้อย! ส่งเรื่องให้ฝ่ายบัญชีตรวจสอบแล้ว", slipUrl });
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

    public IActionResult Admin()
    {
        ViewData["Title"] = "Dashboard";
        return View();
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
