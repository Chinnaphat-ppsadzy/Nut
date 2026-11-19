using System;
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
    [RequireRole("ACCOUNTING", "ADMIN")]
    public class AccountingController : Controller
    {
        private readonly Csi402dbContext _db;
        private readonly IWebHostEnvironment _env;

        public AccountingController(Csi402dbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // ============================================================
        // 1. SLIP VERIFICATION & BOOKING APPROVAL (ตรวจสอบสลิป & อนุมัติคิว)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Index(string? filter, string? search)
        {
            ViewData["Title"] = "ตรวจสอบสลิปโอนเงิน & อนุมัติยืนยันคิว";
            await EnsureAccountingRoleAndUserAsync();

            var query = _db.Bookings
                .Include(b => b.User)
                .Include(b => b.Payments)
                .Include(b => b.BookingSlots).ThenInclude(bs => bs.Court)
                .Include(b => b.BookingSlots).ThenInclude(bs => bs.Slot)
                .Include(b => b.BookingItems).ThenInclude(bi => bi.Product)
                .Include(b => b.Receipts)
                .AsQueryable();

            string currentFilter = filter?.ToUpper() ?? "PENDING";
            ViewBag.CurrentFilter = currentFilter;

            if (currentFilter == "PENDING")
            {
                query = query.Where(b => b.Status == "PENDING_VERIFY");
            }
            else if (currentFilter == "CONFIRMED")
            {
                query = query.Where(b => b.Status == "CONFIRMED" || b.Status == "CHECKED_IN" || b.Status == "COMPLETED");
            }
            else if (currentFilter == "REJECTED")
            {
                query = query.Where(b => b.Status == "REJECTED" || b.Status == "CANCELLED");
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim().ToLower();
                query = query.Where(b => b.BookingCode.ToLower().Contains(s)
                                      || (b.User != null && b.User.FullName.ToLower().Contains(s))
                                      || (b.User != null && b.User.Phone != null && b.User.Phone.Contains(s)));
                ViewBag.SearchTerm = search.Trim();
            }

            var bookings = await query
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            var walkInSlips = await _db.WalkInQueues
                .Include(q => q.Court)
                .Include(q => q.Slot)
                .Where(q => q.PaymentStatus == "PENDING_VERIFY" && q.SlipImageUrl != null)
                .OrderBy(q => q.PaidAt)
                .ToListAsync();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                walkInSlips = walkInSlips.Where(q => q.CustomerName.ToLower().Contains(term) ||
                    (q.Phone != null && q.Phone.Contains(term)) || $"W-{q.QueueNo:D2}".ToLower().Contains(term)).ToList();
            }
            ViewBag.WalkInPendingPayments = walkInSlips;

            // สถิติด่วนสำหรับหัวข้อ
            ViewBag.PendingCount = await _db.Bookings.CountAsync(b => b.Status == "PENDING_VERIFY");
            ViewBag.ConfirmedCount = await _db.Bookings.CountAsync(b => b.Status == "CONFIRMED" || b.Status == "CHECKED_IN" || b.Status == "COMPLETED");
            ViewBag.RejectedCount = await _db.Bookings.CountAsync(b => b.Status == "REJECTED");
            ViewBag.TotalCount = await _db.Bookings.CountAsync();

            return View(bookings);
        }

        [HttpGet]
        public async Task<IActionResult> WalkInSlip(uint queueId)
        {
            await EnsureAccountingRoleAndUserAsync();
            var slipPath = await _db.WalkInQueues.Where(q => q.QueueId == queueId)
                .Select(q => q.SlipImageUrl).FirstOrDefaultAsync();
            if (string.IsNullOrWhiteSpace(slipPath)) return NotFound();
            var fileName = Path.GetFileName(slipPath);
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var contentType = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => null
            };
            if (contentType == null) return NotFound();
            var fullPath = Path.Combine(_env.WebRootPath, "uploads", "slips", fileName);
            if (!System.IO.File.Exists(fullPath)) return NotFound();
            Response.Headers.CacheControl = "no-store, private";
            return PhysicalFile(fullPath, contentType);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveWalkInSlip(uint queueId)
        {
            await EnsureAccountingRoleAndUserAsync();
            var queue = await _db.WalkInQueues.FirstOrDefaultAsync(q => q.QueueId == queueId);
            if (queue == null) return Json(new { success = false, message = "ไม่พบคิว Walk-in" });
            if (queue.PaymentStatus != "PENDING_VERIFY" || string.IsNullOrWhiteSpace(queue.SlipImageUrl))
                return Json(new { success = false, message = "คิวนี้ไม่มีสลิปรอตรวจสอบ" });

            queue.PaymentStatus = "PAID";
            queue.PaidAt = DateTime.Now;
            queue.PaidBy = await GetCurrentAccountantIdAsync();
            await _db.SaveChangesAsync();
            return Json(new { success = true, message = $"อนุมัติสลิปคิว W-{queue.QueueNo:D2} และยืนยันการชำระแล้ว" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectWalkInSlip(uint queueId, string? reason)
        {
            await EnsureAccountingRoleAndUserAsync();
            var queue = await _db.WalkInQueues.FirstOrDefaultAsync(q => q.QueueId == queueId);
            if (queue == null) return Json(new { success = false, message = "ไม่พบคิว Walk-in" });
            if (queue.PaymentStatus != "PENDING_VERIFY")
                return Json(new { success = false, message = "คิวนี้ไม่ได้อยู่ในสถานะรอตรวจสลิป" });

            queue.PaymentStatus = "REJECTED";
            queue.PaidAt = null;
            queue.PaidBy = null;
            await _db.SaveChangesAsync();
            return Json(new { success = true, message = $"ปฏิเสธสลิปคิว W-{queue.QueueNo:D2} ลูกค้าสามารถแนบสลิปใหม่ได้" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveBooking(uint bookingId)
        {
            try
            {
                var booking = await _db.Bookings
                    .Include(b => b.Payments)
                    .Include(b => b.User)
                    .FirstOrDefaultAsync(b => b.BookingId == bookingId);

                if (booking == null)
                {
                    return Json(new { success = false, message = "ไม่พบข้อมูลการจองในระบบ" });
                }

                if (booking.Status != "PENDING_VERIFY")
                {
                    return Json(new { success = false, message = "รายการนี้ไม่ได้อยู่ในสถานะรอตรวจสอบสลิป หรือได้รับการดำเนินการแล้ว" });
                }

                uint accountantId = await GetCurrentAccountantIdAsync();
                var now = DateTime.Now;

                // 1. อัปเดตสถานะการจองเป็น CONFIRMED
                booking.Status = "CONFIRMED";
                booking.ConfirmedAt = now;
                booking.UpdatedAt = now;

                // 2. อัปเดตสถานะ Payment ตาม enum ในฐานข้อมูล
                var payment = booking.Payments.OrderByDescending(p => p.PaymentId).FirstOrDefault();
                if (payment != null)
                {
                    payment.Status = "APPROVED";
                    payment.VerifiedBy = accountantId;
                    payment.VerifiedAt = now;
                    payment.RejectReason = null;
                }
                else
                {
                    return Json(new { success = false, message = "ไม่พบข้อมูลการชำระเงินของรายการนี้ กรุณาให้ลูกค้าแนบสลิปก่อนอนุมัติ" });
                }

                // Persist the approval first so the payment has a database ID for the receipt FK.
                await _db.SaveChangesAsync();

                // 3. ออกใบเสร็จรับเงินอัตโนมัติ (หากยังไม่มี)
                var existingReceipt = await _db.Receipts.FirstOrDefaultAsync(r => r.BookingId == booking.BookingId);
                if (existingReceipt == null)
                {
                    decimal net = booking.NetAmount;
                    decimal vat = Math.Round(net * 7m / 107m, 2);
                    decimal amountBeforeVat = net - vat;

                    var receipt = new Receipt
                    {
                        ReceiptNo = $"RC-{now:yyyyMMdd}-{booking.BookingId:D4}",
                        BookingId = booking.BookingId,
                        PaymentId = payment.PaymentId,
                        DocType = "RECEIPT",
                        CustomerName = booking.User?.FullName ?? "คุณลูกค้า",
                        Amount = amountBeforeVat,
                        VatAmount = vat,
                        IssuedBy = accountantId,
                        IssuedAt = now
                    };
                    _db.Receipts.Add(receipt);
                }

                await _db.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"อนุมัติสลิปและยืนยันคิว {booking.BookingCode} เรียบร้อยแล้ว! ออกใบเสร็จรับเงินให้อัตโนมัติ"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "เกิดข้อผิดพลาดในการอนุมัติ: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectSlip(uint bookingId, string reason)
        {
            try
            {
                var booking = await _db.Bookings
                    .Include(b => b.Payments)
                    .FirstOrDefaultAsync(b => b.BookingId == bookingId);

                if (booking == null)
                {
                    return Json(new { success = false, message = "ไม่พบข้อมูลการจองในระบบ" });
                }

                booking.Status = "REJECTED";
                booking.CancelReason = string.IsNullOrWhiteSpace(reason) ? "สลิปโอนเงินไม่ถูกต้อง หรือยอดเงินไม่ตรง" : reason.Trim();
                booking.UpdatedAt = DateTime.Now;

                var payment = booking.Payments.OrderByDescending(p => p.PaymentId).FirstOrDefault();
                if (payment != null)
                {
                    payment.Status = "REJECTED";
                }

                await _db.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"ปฏิเสธสลิปของรายการ {booking.BookingCode} เรียบร้อยแล้ว ลูกค้าสามารถอัปโหลดสลิปใหม่ได้"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "เกิดข้อผิดพลาดในการปฏิเสธ: " + ex.Message });
            }
        }

        // ============================================================
        // 2. RECEIPTS & TAX INVOICES (ออกใบเสร็จรับเงิน / ใบกำกับภาษี)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Receipts(string? search, string? docType)
        {
            ViewData["Title"] = "ออกใบเสร็จรับเงิน & ใบกำกับภาษี";
            await EnsureAccountingRoleAndUserAsync();

            var query = _db.Receipts
                .Include(r => r.Booking).ThenInclude(b => b.User)
                .Include(r => r.Booking).ThenInclude(b => b.BookingSlots).ThenInclude(bs => bs.Court)
                .Include(r => r.Booking).ThenInclude(b => b.BookingItems).ThenInclude(bi => bi.Product)
                .Include(r => r.IssuedByNavigation)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(docType) && docType != "ALL")
            {
                query = query.Where(r => r.DocType == docType);
                ViewBag.ActiveDocType = docType;
            }
            else
            {
                ViewBag.ActiveDocType = "ALL";
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim().ToLower();
                query = query.Where(r => r.ReceiptNo.ToLower().Contains(s)
                                      || r.CustomerName.ToLower().Contains(s)
                                      || r.Booking.BookingCode.ToLower().Contains(s)
                                      || (r.TaxId != null && r.TaxId.Contains(s)));
                ViewBag.SearchTerm = search.Trim();
            }

            var receipts = await query
                .OrderByDescending(r => r.IssuedAt)
                .ToListAsync();

            return View(receipts);
        }

        [HttpPost]
        public async Task<IActionResult> IssueTaxInvoice(uint receiptId, string companyName, string taxId, string address)
        {
            try
            {
                var receipt = await _db.Receipts.FindAsync(receiptId);
                if (receipt == null)
                {
                    TempData["ErrorMessage"] = "ไม่พบใบเสร็จรับเงิน";
                    return RedirectToAction(nameof(Receipts));
                }

                receipt.DocType = "TAX_INVOICE";
                receipt.CustomerName = string.IsNullOrWhiteSpace(companyName) ? receipt.CustomerName : companyName.Trim();
                receipt.TaxId = taxId?.Trim();
                receipt.Address = address?.Trim();
                receipt.ReceiptNo = receipt.ReceiptNo.Replace("RC-", "TAX-");
                receipt.IssuedAt = DateTime.Now;

                await _db.SaveChangesAsync();

                TempData["SuccessMessage"] = $"ออกใบกำกับภาษีเต็มรูป {receipt.ReceiptNo} ให้ {receipt.CustomerName} สำเร็จ!";
                return RedirectToAction(nameof(Receipts));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "เกิดข้อผิดพลาดในการออกใบกำกับภาษี: " + ex.Message;
                return RedirectToAction(nameof(Receipts));
            }
        }

        [HttpGet]
        public async Task<IActionResult> PrintReceipt(uint id)
        {
            var receipt = await _db.Receipts
                .Include(r => r.Booking).ThenInclude(b => b.User)
                .Include(r => r.Booking).ThenInclude(b => b.BookingSlots).ThenInclude(bs => bs.Court)
                .Include(r => r.Booking).ThenInclude(b => b.BookingSlots).ThenInclude(bs => bs.Slot)
                .Include(r => r.Booking).ThenInclude(b => b.BookingItems).ThenInclude(bi => bi.Product)
                .Include(r => r.IssuedByNavigation)
                .FirstOrDefaultAsync(r => r.ReceiptId == id);

            if (receipt == null)
            {
                return NotFound();
            }

            return View(receipt);
        }

        // ============================================================
        // 3. DAMAGES & REFUNDS (คิดค่าปรับอุปกรณ์ชำรุด & จัดการคืนเงิน)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> DamagesAndRefunds(string? tab)
        {
            ViewData["Title"] = "คิดค่าปรับอุปกรณ์เสียหาย & จัดการคืนเงิน";
            await EnsureAccountingRoleAndUserAsync();

            ViewBag.ActiveTab = tab ?? "damages";

            // 1. ข้อมูลความเสียหายอุปกรณ์
            var damages = await _db.EquipmentDamages
                .Include(d => d.BookingItem).ThenInclude(bi => bi.Product)
                .Include(d => d.BookingItem).ThenInclude(bi => bi.Booking).ThenInclude(b => b.User)
                .Include(d => d.ReportedByNavigation)
                .Include(d => d.ProcessedByNavigation)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            ViewBag.Damages = damages;

            // 2. ข้อมูลรายการคืนเงิน (Refunds)
            var refunds = await _db.Refunds
                .Include(r => r.Booking).ThenInclude(b => b.User)
                .Include(r => r.Payment)
                .Include(r => r.ProcessedByNavigation)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.Refunds = refunds;

            // 3. รายการจองที่ลูกค้ายกเลิกที่อาจต้องคืนเงิน
            var cancelledBookings = await _db.Bookings
                .Include(b => b.User)
                .Include(b => b.Payments)
                .Include(b => b.Refunds)
                .Where(b => (b.Status == "CANCELLED" || b.Status == "REJECTED") && b.Payments.Any(p => p.Status == "PAID"))
                .OrderByDescending(b => b.UpdatedAt)
                .ToListAsync();

            ViewBag.CancelledBookings = cancelledBookings;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDamageFine(uint damageId, decimal fineAmount, string status)
        {
            try
            {
                var damage = await _db.EquipmentDamages.FindAsync(damageId);
                if (damage == null)
                {
                    TempData["ErrorMessage"] = "ไม่พบรายการอุปกรณ์เสียหาย";
                    return RedirectToAction(nameof(DamagesAndRefunds), new { tab = "damages" });
                }

                uint accountantId = await GetCurrentAccountantIdAsync();

                damage.FineAmount = fineAmount;
                damage.Status = status.ToUpper();
                damage.ProcessedBy = accountantId;
                damage.ProcessedAt = DateTime.Now;

                await _db.SaveChangesAsync();

                string statusText = damage.Status switch
                {
                    "CHARGED" => "คิดค่าปรับเรียบร้อย",
                    "PAID" => "บันทึกการชำระค่าปรับแล้ว",
                    "WAIVED" => "ยกเว้นค่าปรับ",
                    _ => damage.Status
                };

                TempData["SuccessMessage"] = $"บันทึกรายการค่าปรับ #{damage.DamageId} เป็น '{statusText}' (จำนวน {damage.FineAmount:N2} ฿) สำเร็จ!";
                return RedirectToAction(nameof(DamagesAndRefunds), new { tab = "damages" });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "เกิดข้อผิดพลาดในการบันทึกค่าปรับ: " + ex.Message;
                return RedirectToAction(nameof(DamagesAndRefunds), new { tab = "damages" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateRefund(uint bookingId, decimal amount, string reason)
        {
            try
            {
                var booking = await _db.Bookings
                    .Include(b => b.Payments)
                    .FirstOrDefaultAsync(b => b.BookingId == bookingId);

                if (booking == null)
                {
                    TempData["ErrorMessage"] = "ไม่พบข้อมูลการจอง";
                    return RedirectToAction(nameof(DamagesAndRefunds), new { tab = "refunds" });
                }

                var payment = booking.Payments.OrderByDescending(p => p.PaymentId).FirstOrDefault();
                if (payment == null)
                {
                    TempData["ErrorMessage"] = "ไม่พบข้อมูลการชำระเงินของบิลนี้";
                    return RedirectToAction(nameof(DamagesAndRefunds), new { tab = "refunds" });
                }

                uint accountantId = await GetCurrentAccountantIdAsync();

                var refund = new Refund
                {
                    BookingId = booking.BookingId,
                    PaymentId = payment.PaymentId,
                    Amount = amount > 0 ? amount : booking.NetAmount,
                    Reason = string.IsNullOrWhiteSpace(reason) ? "ลูกค้ายกเลิกการจองตามเงื่อนไข" : reason.Trim(),
                    Status = "COMPLETED",
                    ProcessedBy = accountantId,
                    ProcessedAt = DateTime.Now,
                    CreatedAt = DateTime.Now
                };

                _db.Refunds.Add(refund);

                booking.Status = "CANCELLED";
                booking.UpdatedAt = DateTime.Now;

                await _db.SaveChangesAsync();

                TempData["SuccessMessage"] = $"บันทึกการคืนเงิน {refund.Amount:N2} ฿ สำหรับบิล {booking.BookingCode} สำเร็จเรียบร้อย!";
                return RedirectToAction(nameof(DamagesAndRefunds), new { tab = "refunds" });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "เกิดข้อผิดพลาดในการคืนเงิน: " + ex.Message;
                return RedirectToAction(nameof(DamagesAndRefunds), new { tab = "refunds" });
            }
        }

        // ============================================================
        // 4. SALES & REVENUE REPORTS (รายงานสรุปยอดขายรายวัน/รายเดือน)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Reports(string? reportType, string? date, int? month, int? year)
        {
            ViewData["Title"] = "รายงานสรุปยอดขาย & รายได้";
            await EnsureAccountingRoleAndUserAsync();

            string currentType = reportType?.ToLower() ?? "daily";
            ViewBag.ReportType = currentType;

            int currentYear = year ?? DateTime.Today.Year;
            int currentMonth = month ?? DateTime.Today.Month;
            string selectedDateStr = string.IsNullOrWhiteSpace(date) ? DateTime.Today.ToString("yyyy-MM-dd") : date.Trim();

            ViewBag.SelectedYear = currentYear;
            ViewBag.SelectedMonth = currentMonth;
            ViewBag.SelectedDate = selectedDateStr;

            if (currentType == "daily")
            {
                DateOnly targetDate = DateOnly.TryParse(selectedDateStr, out var pd) ? pd : DateOnly.FromDateTime(DateTime.Today);

                var dailyBookings = await _db.Bookings
                    .Include(b => b.User)
                    .Include(b => b.BookingSlots).ThenInclude(bs => bs.Court)
                    .Include(b => b.BookingItems).ThenInclude(bi => bi.Product)
                    .Where(b => b.PlayDate == targetDate && (b.Status == "CONFIRMED" || b.Status == "CHECKED_IN" || b.Status == "COMPLETED" || b.Status == "IN_USE"))
                    .OrderBy(b => b.CreatedAt)
                    .ToListAsync();

                var dailyWalkIns = await _db.WalkInQueues
                    .Include(q => q.Court).Include(q => q.Slot)
                    .Where(q => q.QueueDate == targetDate && q.PaymentStatus == "PAID")
                    .OrderBy(q => q.PaidAt)
                    .ToListAsync();

                var dayStart = targetDate.ToDateTime(TimeOnly.MinValue);
                var nextDayStart = targetDate.AddDays(1).ToDateTime(TimeOnly.MinValue);
                var dailyPosSales = await _db.PosSaleTransactions
                    .Where(s => s.PaymentStatus == "PAID" && s.PaidAt >= dayStart && s.PaidAt < nextDayStart)
                    .OrderBy(s => s.PaidAt)
                    .ToListAsync();

                decimal totalCourt = dailyBookings.Sum(b => b.CourtAmount) + dailyWalkIns.Sum(q => q.CourtFee);
                decimal totalSale = dailyBookings.Sum(b => b.SaleAmount) + dailyPosSales.Sum(s => s.SaleAmount);
                decimal totalRent = dailyBookings.Sum(b => b.RentalAmount) + dailyPosSales.Sum(s => s.RentalAmount);
                decimal totalDiscount = dailyBookings.Sum(b => b.DiscountAmount) + dailyPosSales.Sum(s => s.DiscountAmount);
                decimal netTotal = dailyBookings.Sum(b => b.NetAmount) + dailyWalkIns.Sum(q => q.CourtFee) + dailyPosSales.Sum(s => s.NetTotal);

                ViewBag.TotalCourt = totalCourt;
                ViewBag.TotalSale = totalSale;
                ViewBag.TotalRent = totalRent;
                ViewBag.TotalDiscount = totalDiscount;
                ViewBag.NetTotal = netTotal;
                ViewBag.TransactionsCount = dailyBookings.Count + dailyWalkIns.Count + dailyPosSales.Count;
                ViewBag.WalkInPayments = dailyWalkIns;
                ViewBag.WalkInTotal = dailyWalkIns.Sum(q => q.CourtFee);
                ViewBag.PosSales = dailyPosSales;

                return View(dailyBookings);
            }
            else
            {
                // รายงานรายเดือน (Monthly Report)
                DateOnly startMonth = new DateOnly(currentYear, currentMonth, 1);
                DateOnly endMonth = startMonth.AddMonths(1).AddDays(-1);

                var monthlyBookings = await _db.Bookings
                    .Include(b => b.User)
                    .Include(b => b.BookingSlots).ThenInclude(bs => bs.Court)
                    .Include(b => b.BookingItems).ThenInclude(bi => bi.Product)
                    .Where(b => b.PlayDate >= startMonth && b.PlayDate <= endMonth && (b.Status == "CONFIRMED" || b.Status == "CHECKED_IN" || b.Status == "COMPLETED" || b.Status == "IN_USE"))
                    .OrderBy(b => b.PlayDate)
                    .ToListAsync();

                var monthlyWalkIns = await _db.WalkInQueues
                    .Include(q => q.Court).Include(q => q.Slot)
                    .Where(q => q.QueueDate >= startMonth && q.QueueDate <= endMonth && q.PaymentStatus == "PAID")
                    .OrderBy(q => q.QueueDate).ThenBy(q => q.PaidAt)
                    .ToListAsync();

                var monthStartDate = startMonth.ToDateTime(TimeOnly.MinValue);
                var monthEndExclusive = endMonth.AddDays(1).ToDateTime(TimeOnly.MinValue);
                var monthlyPosSales = await _db.PosSaleTransactions
                    .Where(s => s.PaymentStatus == "PAID" && s.PaidAt >= monthStartDate && s.PaidAt < monthEndExclusive)
                    .OrderBy(s => s.PaidAt)
                    .ToListAsync();

                decimal totalCourt = monthlyBookings.Sum(b => b.CourtAmount) + monthlyWalkIns.Sum(q => q.CourtFee);
                decimal totalSale = monthlyBookings.Sum(b => b.SaleAmount) + monthlyPosSales.Sum(s => s.SaleAmount);
                decimal totalRent = monthlyBookings.Sum(b => b.RentalAmount) + monthlyPosSales.Sum(s => s.RentalAmount);
                decimal totalDiscount = monthlyBookings.Sum(b => b.DiscountAmount) + monthlyPosSales.Sum(s => s.DiscountAmount);
                decimal netTotal = monthlyBookings.Sum(b => b.NetAmount) + monthlyWalkIns.Sum(q => q.CourtFee) + monthlyPosSales.Sum(s => s.NetTotal);

                ViewBag.TotalCourt = totalCourt;
                ViewBag.TotalSale = totalSale;
                ViewBag.TotalRent = totalRent;
                ViewBag.TotalDiscount = totalDiscount;
                ViewBag.NetTotal = netTotal;
                ViewBag.TransactionsCount = monthlyBookings.Count + monthlyWalkIns.Count + monthlyPosSales.Count;
                ViewBag.WalkInPayments = monthlyWalkIns;
                ViewBag.WalkInTotal = monthlyWalkIns.Sum(q => q.CourtFee);
                ViewBag.PosSales = monthlyPosSales;

                // Group by Day for chart/summary
                var dailyGroups = monthlyBookings
                    .Select(b => new { Day = b.PlayDate.Day, Court = b.CourtAmount, Sale = b.SaleAmount, Rent = b.RentalAmount, Net = b.NetAmount, Count = 1 })
                    .Concat(monthlyWalkIns.Select(q => new { Day = q.QueueDate.Day, Court = q.CourtFee, Sale = 0m, Rent = 0m, Net = q.CourtFee, Count = 1 }))
                    .Concat(monthlyPosSales.Select(s => new { Day = s.PaidAt.Day, Court = 0m, Sale = s.SaleAmount, Rent = s.RentalAmount, Net = s.NetTotal, Count = 1 }))
                    .GroupBy(x => x.Day)
                    .Select(g => new { Day = g.Key, Court = g.Sum(x => x.Court), Sale = g.Sum(x => x.Sale), Rent = g.Sum(x => x.Rent), Net = g.Sum(x => x.Net), Count = g.Sum(x => x.Count) })
                    .OrderBy(g => g.Day)
                    .ToList();

                ViewBag.DailyGroups = dailyGroups;

                return View(monthlyBookings);
            }
        }

        // ============================================================
        // PRIVATE HELPERS
        // ============================================================
        private async Task<uint> GetCurrentAccountantIdAsync()
        {
            string? uidStr = HttpContext.Session.GetString("UserId") ?? Request.Cookies["cb_user_id"];
            if (!string.IsNullOrEmpty(uidStr) && uint.TryParse(uidStr, out uint uid))
            {
                var existingUser = await _db.Users.FindAsync(uid);
                if (existingUser != null) return uid;
            }

            var accountant = await _db.Users.FirstOrDefaultAsync(u => u.Role.RoleCode == "ACCOUNTING");
            if (accountant != null) return accountant.UserId;

            var firstUser = await _db.Users.FirstOrDefaultAsync();
            return firstUser?.UserId ?? 1;
        }

        private async Task EnsureAccountingRoleAndUserAsync()
        {
            // ตรวจสอบ Role ACCOUNTING ในตาราง roles (ไม่สร้าง User ซ้ำอัตโนมัติ)
            var accountingRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleCode == "ACCOUNTING");
            if (accountingRole == null)
            {
                accountingRole = new Role
                {
                    RoleCode = "ACCOUNTING",
                    RoleName = "ฝ่ายบัญชีและการเงิน",
                    Description = "ตรวจสอบสลิป อนุมัติยืนยันคิว ออกใบเสร็จ/ใบกำกับภาษี จัดการคืนเงินและค่าปรับ สรุปรายงานรายได้"
                };
                _db.Roles.Add(accountingRole);
                await _db.SaveChangesAsync();
            }
        }
    }
}
