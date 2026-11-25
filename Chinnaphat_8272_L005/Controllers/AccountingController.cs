using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Chinnaphat_8272_L005.Models;
using Chinnaphat_8272_L005.Helpers;
using Chinnaphat_8272_L005.Database;

namespace Chinnaphat_8272_L005.Controllers
{
    [RequireRole("ACCOUNTING")]
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

            var posPayments = await _db.PosSaleTransactions
                .Where(s => s.PaymentStatus == "PENDING_VERIFY" && s.SlipImageUrl != null)
                .OrderBy(s => s.CreatedAt)
                .ToListAsync();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                posPayments = posPayments.Where(s => s.ReceiptCode.ToLower().Contains(term) || s.CustomerName.ToLower().Contains(term) || (s.CustomerPhone != null && s.CustomerPhone.Contains(term))).ToList();
            }
            ViewBag.PosPendingPayments = posPayments;

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
            var fullPath = UploadStorage.GetSlipPath(_env, slipPath);
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

        [HttpGet]
        public async Task<IActionResult> PosSaleSlip(uint saleId)
        {
            await EnsureAccountingRoleAndUserAsync();
            var slipUrl = await _db.PosSaleTransactions.Where(s => s.SaleId == saleId).Select(s => s.SlipImageUrl).FirstOrDefaultAsync();
            if (string.IsNullOrWhiteSpace(slipUrl)) return NotFound();
            var extension = Path.GetExtension(slipUrl).ToLowerInvariant();
            var contentType = extension switch { ".jpg" or ".jpeg" => "image/jpeg", ".png" => "image/png", ".webp" => "image/webp", _ => null };
            if (contentType == null) return NotFound();
            var fullPath = UploadStorage.GetSlipPath(_env, slipUrl);
            if (!System.IO.File.Exists(fullPath)) return NotFound();
            Response.Headers.CacheControl = "no-store, private";
            return PhysicalFile(fullPath, contentType);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApprovePosSale(uint saleId)
        {
            await EnsureAccountingRoleAndUserAsync();
            var sale = await _db.PosSaleTransactions.FirstOrDefaultAsync(s => s.SaleId == saleId);
            if (sale == null) return Json(new { success = false, message = "ไม่พบรายการขาย" });
            if (sale.PaymentStatus != "PENDING_VERIFY" || string.IsNullOrWhiteSpace(sale.SlipImageUrl))
                return Json(new { success = false, message = "รายการนี้ไม่มีสลิปรอตรวจสอบ" });
            sale.PaymentStatus = "PAID";
            sale.PaidAt = DateTime.Now;
            sale.VerifiedAt = DateTime.Now;
            sale.VerifiedBy = await GetCurrentAccountantIdAsync();
            sale.RejectReason = null;
            await _db.SaveChangesAsync();
            return Json(new { success = true, message = $"อนุมัติสลิป {sale.ReceiptCode} ยอด {sale.NetTotal:N2} บาทแล้ว และบันทึกรายรับเรียบร้อย" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectPosSale(uint saleId, string? reason)
        {
            await EnsureAccountingRoleAndUserAsync();
            await using var transaction = await _db.Database.BeginTransactionAsync();
            var sale = await _db.PosSaleTransactions.FirstOrDefaultAsync(s => s.SaleId == saleId);
            if (sale == null) return Json(new { success = false, message = "ไม่พบรายการขาย" });
            if (sale.PaymentStatus != "PENDING_VERIFY") return Json(new { success = false, message = "รายการนี้ผ่านการตรวจหรือถูกปฏิเสธไปแล้ว" });
            if (!string.IsNullOrWhiteSpace(sale.ItemsJson))
            {
                var items = System.Text.Json.JsonSerializer.Deserialize<List<PosCartItemDto>>(sale.ItemsJson) ?? new();
                foreach (var item in items)
                {
                    var product = await _db.Products.FirstOrDefaultAsync(p => p.ProductId == (uint)item.ProductId);
                    if (product != null) { product.StockQty += item.Qty; product.UpdatedAt = DateTime.Now; }
                }
            }
            sale.PaymentStatus = "REJECTED";
            sale.RejectReason = string.IsNullOrWhiteSpace(reason) ? "สลิปไม่ผ่านการตรวจสอบ" : reason.Trim()[..Math.Min(reason.Trim().Length, 500)];
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Json(new { success = true, message = $"ปฏิเสธรายการ {sale.ReceiptCode} และคืนสต็อกแล้ว" });
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
            await WalkInPaymentSchemaInitializer.EnsureRentalDepositRefundTableAsync(_db);
            await BookingRefundSchemaInitializer.EnsureAsync(_db);

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

            // POS rental deposit refunds are stored with each sale line in the existing items_json column.
            var posDepositRefunds = new List<PosDepositRefundViewModel>();
            var paidPosSales = await _db.PosSaleTransactions
                .Where(s => s.PaymentStatus == "PAID")
                .OrderByDescending(s => s.CreatedAt)
                .Take(250)
                .ToListAsync();
            foreach (var sale in paidPosSales)
            {
                if (string.IsNullOrWhiteSpace(sale.ItemsJson)) continue;
                var items = System.Text.Json.JsonSerializer.Deserialize<List<PosCartItemDto>>(sale.ItemsJson) ?? new();
                foreach (var item in items.Where(i => i.ProductType == "RENT"))
                {
                    foreach (var request in item.DepositRefunds)
                    {
                        posDepositRefunds.Add(new PosDepositRefundViewModel
                        {
                            Sale = sale,
                            Item = item,
                            Request = request
                        });
                    }
                }
            }
            ViewBag.PosDepositRefunds = posDepositRefunds.OrderByDescending(r => r.Request.CreatedAt).ToList();

            var rentalDepositRequests = await _db.RentalDepositRefundRequests
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            ViewBag.RentalDepositRefundRequests = rentalDepositRequests;
            ViewBag.PosDamageRequests = rentalDepositRequests
                .Where(r => r.SaleId.HasValue && !string.IsNullOrWhiteSpace(r.DamageDescription))
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
            var onlineBookingItemIds = rentalDepositRequests
                .Where(r => r.BookingItemId.HasValue)
                .Select(r => r.BookingItemId!.Value)
                .Distinct()
                .ToList();
            ViewBag.DepositFineByBookingItem = await _db.EquipmentDamages
                .Where(d => onlineBookingItemIds.Contains(d.BookingItemId) && d.Status != "REPORTED")
                .GroupBy(d => d.BookingItemId)
                .Select(g => new { BookingItemId = g.Key, Fine = g.Sum(d => d.FineAmount) })
                .ToDictionaryAsync(x => x.BookingItemId, x => x.Fine);

            // 2. ข้อมูลรายการคืนเงิน (Refunds)
            var refunds = await _db.Refunds
                .Include(r => r.Booking).ThenInclude(b => b.User)
                .Include(r => r.Payment)
                .Include(r => r.ProcessedByNavigation)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.Refunds = refunds;

            ViewBag.BookingRefundRequests = await _db.BookingRefundRequests
                .Where(r => r.BookingItemId.HasValue || r.SaleId.HasValue)
                .OrderByDescending(r => r.CreatedAt).ToListAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessBookingRefundRequest(uint requestId, bool approve, string? refundMethod, string? refundReference, string? accountingNote, string? rejectNote)
        {
            await BookingRefundSchemaInitializer.EnsureAsync(_db);
            var request = await _db.BookingRefundRequests.FirstOrDefaultAsync(r => r.RequestId == requestId && r.Status == "PENDING_ACCOUNTING");
            if (request == null) { TempData["ErrorMessage"] = "ไม่พบคำขอที่รอฝ่ายบัญชี"; return RedirectToAction(nameof(DamagesAndRefunds), new { tab = "refunds" }); }
            var note = (approve ? accountingNote : rejectNote ?? accountingNote ?? string.Empty)?.Trim() ?? string.Empty;
            if (note.Length > 500) { TempData["ErrorMessage"] = "บันทึกได้ไม่เกิน 500 ตัวอักษร"; return RedirectToAction(nameof(DamagesAndRefunds), new { tab = "refunds" }); }
            if (!approve)
            {
                if (note.Length < 3) { TempData["ErrorMessage"] = "กรุณาระบุเหตุผลที่ปฏิเสธ"; return RedirectToAction(nameof(DamagesAndRefunds), new { tab = "refunds" }); }
                request.Status = "REJECTED";
                request.AccountingNote = note;
            }
            else
            {
                refundMethod = (refundMethod ?? string.Empty).Trim();
                refundReference = (refundReference ?? string.Empty).Trim();
                if (refundMethod.Length > 40 || refundReference.Length > 100)
                { TempData["ErrorMessage"] = "ช่องทางคืนเงินไม่เกิน 40 ตัวอักษร และเลขอ้างอิงไม่เกิน 100 ตัวอักษร"; return RedirectToAction(nameof(DamagesAndRefunds), new { tab = "refunds" }); }
                if (refundMethod.Length < 2 || refundReference.Length < 2)
                { TempData["ErrorMessage"] = "เมื่อคืนเงินจริง กรุณาบันทึกช่องทางและเลขอ้างอิง"; return RedirectToAction(nameof(DamagesAndRefunds), new { tab = "refunds" }); }

                if (request.BookingItemId.HasValue && request.BookingId.HasValue)
                {
                    var item = await _db.BookingItems.Include(i => i.Booking).ThenInclude(b => b.Payments)
                        .FirstOrDefaultAsync(i => i.BookingItemId == request.BookingItemId.Value && i.BookingId == request.BookingId.Value && i.ItemType == "SALE");
                    var booking = item?.Booking;
                    if (item == null || booking == null || item.LineTotal != request.Amount)
                    { TempData["ErrorMessage"] = "ไม่พบสินค้าที่ขอคืน หรือยอดสินค้าไม่ตรง"; return RedirectToAction(nameof(DamagesAndRefunds), new { tab = "refunds" }); }
                    var paid = booking.Payments.Where(p => p.Status is "APPROVED" or "PAID").OrderByDescending(p => p.PaymentId).ToList();
                    var paidTotal = paid.Sum(p => p.Amount);
                    var alreadyRefunded = await _db.Refunds.Where(r => r.BookingId == booking.BookingId).SumAsync(r => (decimal?)r.Amount) ?? 0m;
                    var duplicateProductRefund = await _db.BookingRefundRequests.AnyAsync(r => r.RequestId != requestId && r.BookingItemId == item.BookingItemId && r.Status == "COMPLETED");
                    if (paidTotal <= 0 || duplicateProductRefund || paidTotal - alreadyRefunded < request.Amount)
                    { TempData["ErrorMessage"] = "ไม่พบยอดชำระคงเหลือพอคืน หรือสินค้านี้คืนเงินไปแล้ว"; return RedirectToAction(nameof(DamagesAndRefunds), new { tab = "refunds" }); }
                    var now = DateTime.Now;
                    var accountantId = await GetCurrentAccountantIdAsync();
                    _db.Refunds.Add(new Refund { BookingId = booking.BookingId, PaymentId = paid[0].PaymentId, Amount = request.Amount, Reason = $"คืนสินค้า {request.ProductName}", Status = "REFUNDED", ProcessedBy = accountantId, ProcessedAt = now, CreatedAt = now });
                }
                else if (request.SaleId.HasValue && request.ProductId.HasValue)
                {
                    var sale = await _db.PosSaleTransactions.FirstOrDefaultAsync(s => s.SaleId == request.SaleId.Value && s.PaymentStatus == "PAID");
                    var lines = sale == null || string.IsNullOrWhiteSpace(sale.ItemsJson)
                        ? new List<PosCartItemDto>()
                        : System.Text.Json.JsonSerializer.Deserialize<List<PosCartItemDto>>(sale.ItemsJson) ?? new();
                    var line = lines.FirstOrDefault(i => i.ProductId == (int)request.ProductId.Value && i.ProductType == "SALE");
                    if (sale == null || line == null)
                    { TempData["ErrorMessage"] = "ไม่พบรายการสินค้าที่ชำระแล้วใน POS"; return RedirectToAction(nameof(DamagesAndRefunds), new { tab = "refunds" }); }
                    var gross = line.Price * line.Qty;
                    var grossAll = sale.SaleAmount + sale.RentalAmount;
                    var expected = Math.Max(0m, decimal.Round(gross - (grossAll > 0 ? sale.DiscountAmount * gross / grossAll : 0m), 2, MidpointRounding.AwayFromZero));
                    var alreadyReturned = await _db.BookingRefundRequests.AnyAsync(r => r.RequestId != requestId && r.SaleId == request.SaleId && r.ProductId == request.ProductId && r.Status == "COMPLETED");
                    var refundedSaleTotal = await _db.BookingRefundRequests.Where(r => r.RequestId != requestId && r.SaleId == request.SaleId && r.Status == "COMPLETED").SumAsync(r => (decimal?)r.Amount) ?? 0m;
                    var saleGoodsNet = Math.Max(0m, decimal.Round(sale.SaleAmount - (grossAll > 0 ? sale.DiscountAmount * sale.SaleAmount / grossAll : 0m), 2, MidpointRounding.AwayFromZero));
                    if (alreadyReturned || expected != request.Amount || refundedSaleTotal + request.Amount > saleGoodsNet)
                    { TempData["ErrorMessage"] = "ยอดคืนสินค้าไม่ตรงกับบิล หรือบันทึกคืนสินค้านี้ไปแล้ว"; return RedirectToAction(nameof(DamagesAndRefunds), new { tab = "refunds" }); }
                }
                else { TempData["ErrorMessage"] = "ระบบนี้คืนเงินได้เฉพาะสินค้าที่ซื้อเท่านั้น"; return RedirectToAction(nameof(DamagesAndRefunds), new { tab = "refunds" }); }
                request.Status = "COMPLETED";
                request.RefundMethod = refundMethod;
                request.RefundReference = refundReference;
                request.AccountingNote = note;
                request.ProcessedBy = await GetCurrentAccountantIdAsync();
                request.ProcessedAt = DateTime.Now;
            }
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = approve ? $"บันทึกคืนเงินเต็มจำนวน {request.Amount:N2} ฿ ให้ {request.ReferenceCode} แล้ว" : "ปฏิเสธคำขอ พร้อมบันทึกเหตุผลแล้ว";
            return RedirectToAction(nameof(DamagesAndRefunds), new { tab = "refunds" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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

                status = (status ?? string.Empty).Trim().ToUpperInvariant();
                if (fineAmount < 0 || status is not ("CHARGED" or "PAID" or "WAIVED"))
                {
                    TempData["ErrorMessage"] = "กรุณาระบุยอดค่าปรับและสถานะให้ถูกต้อง";
                    return RedirectToAction(nameof(DamagesAndRefunds), new { tab = "damages" });
                }
                if (damage.Status is "PAID" or "WAIVED")
                {
                    TempData["ErrorMessage"] = "รายการที่ชำระหรือยกเว้นแล้วไม่สามารถแก้ไขซ้ำได้";
                    return RedirectToAction(nameof(DamagesAndRefunds), new { tab = "damages" });
                }
                if (damage.Status == "REPORTED" && status == "PAID")
                {
                    TempData["ErrorMessage"] = "กรุณากำหนดค่าปรับก่อน แล้วจึงบันทึกการชำระเงิน";
                    return RedirectToAction(nameof(DamagesAndRefunds), new { tab = "damages" });
                }
                if (status == "CHARGED" && fineAmount <= 0)
                {
                    TempData["ErrorMessage"] = "กรุณากำหนดค่าปรับมากกว่า 0 บาท หรือเลือกยกเว้นค่าปรับ";
                    return RedirectToAction(nameof(DamagesAndRefunds), new { tab = "damages" });
                }
                if (status == "WAIVED") fineAmount = 0;

                uint accountantId = await GetCurrentAccountantIdAsync();

                damage.FineAmount = fineAmount;
                damage.Status = status.ToUpper();
                damage.ProcessedBy = accountantId;
                damage.ProcessedAt = DateTime.Now;

                var relatedDepositRequests = await _db.RentalDepositRefundRequests
                    .Where(r => r.BookingItemId == damage.BookingItemId && r.Status == "PENDING")
                    .ToListAsync();
                foreach (var request in relatedDepositRequests)
                {
                    request.DamageAssessmentStatus = status == "WAIVED" ? "WAIVED" : "ASSESSED";
                    request.FineAmount = await _db.EquipmentDamages
                        .Where(d => d.BookingItemId == damage.BookingItemId && d.Status != "REPORTED")
                        .SumAsync(d => (decimal?)d.FineAmount) ?? 0m;
                    request.DamageAssessedBy = accountantId;
                    request.DamageAssessedAt = DateTime.Now;
                }

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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPosRentalDepositRefund(uint saleId, int productId, string requestId, decimal refundedAmount, string refundMethod, string? refundReference, bool approve)
        {
            await EnsureAccountingRoleAndUserAsync();
            var sale = await _db.PosSaleTransactions.FirstOrDefaultAsync(s => s.SaleId == saleId);
            if (sale == null || sale.PaymentStatus != "PAID" || string.IsNullOrWhiteSpace(sale.ItemsJson))
            {
                TempData["ErrorMessage"] = "ไม่พบรายการ POS ที่ชำระเงินแล้ว";
                return RedirectToAction(nameof(DamagesAndRefunds), new { tab = "depositRefunds" });
            }

            var items = System.Text.Json.JsonSerializer.Deserialize<List<PosCartItemDto>>(sale.ItemsJson) ?? new();
            var item = items.FirstOrDefault(i => i.ProductId == productId && i.ProductType == "RENT");
            var request = item?.DepositRefunds.FirstOrDefault(r => r.RequestId == requestId);
            if (item == null || request == null || request.Status != "PENDING")
            {
                TempData["ErrorMessage"] = "รายการคืนมัดจำนี้ถูกดำเนินการแล้วหรือไม่ถูกต้อง";
                return RedirectToAction(nameof(DamagesAndRefunds), new { tab = "depositRefunds" });
            }
            if (approve && (refundedAmount <= 0 || refundedAmount > request.Amount))
            {
                TempData["ErrorMessage"] = "ยอดคืนต้องมากกว่า 0 และไม่เกินยอดมัดจำที่ฝ่ายขายแจ้ง";
                return RedirectToAction(nameof(DamagesAndRefunds), new { tab = "depositRefunds" });
            }
            refundMethod = (refundMethod ?? string.Empty).Trim().ToUpperInvariant();
            if (approve && (refundMethod is not ("PROMPTPAY" or "BANK") || string.IsNullOrWhiteSpace(refundReference)))
            {
                TempData["ErrorMessage"] = "กรุณาระบุช่องทางและเลขอ้างอิงการคืนเงินจริง";
                return RedirectToAction(nameof(DamagesAndRefunds), new { tab = "depositRefunds" });
            }

            request.Status = approve ? "COMPLETED" : "REJECTED";
            request.RefundedAmount = approve ? refundedAmount : 0m;
            request.RefundMethod = approve ? refundMethod : null;
            request.RefundReference = approve ? refundReference!.Trim()[..Math.Min(refundReference.Trim().Length, 100)] : null;
            request.ProcessedBy = await GetCurrentAccountantIdAsync();
            request.ProcessedAt = DateTime.Now;
            sale.ItemsJson = System.Text.Json.JsonSerializer.Serialize(items);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = approve
                ? $"บันทึกคืนมัดจำ {sale.ReceiptCode} จำนวน {refundedAmount:N2} ฿ แล้ว (โปรดโอนเงินจริงให้ลูกค้าตามช่องทางที่ตกลง)"
                : $"ปฏิเสธคำขอคืนมัดจำ {sale.ReceiptCode} แล้ว";
            return RedirectToAction(nameof(DamagesAndRefunds), new { tab = "depositRefunds" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePosRentalDamageFine(uint requestId, decimal fineAmount)
        {
            await EnsureAccountingRoleAndUserAsync();
            var request = await _db.RentalDepositRefundRequests
                .FirstOrDefaultAsync(r => r.RequestId == requestId && r.SaleId.HasValue);
            if (request == null || request.Status != "PENDING" || string.IsNullOrWhiteSpace(request.DamageDescription))
            {
                TempData["ErrorMessage"] = "ไม่พบรายการอุปกรณ์ชำรุด POS ที่รอประเมิน";
                return RedirectToAction(nameof(DamagesAndRefunds), new { tab = "damages" });
            }
            if (fineAmount < 0 || fineAmount > request.DepositAmount)
            {
                TempData["ErrorMessage"] = "ค่าปรับต้องอยู่ระหว่าง 0 ถึงยอดมัดจำ";
                return RedirectToAction(nameof(DamagesAndRefunds), new { tab = "damages" });
            }

            request.FineAmount = fineAmount;
            request.DamageAssessmentStatus = fineAmount > 0 ? "ASSESSED" : "WAIVED";
            request.DamageAssessedBy = await GetCurrentAccountantIdAsync();
            request.DamageAssessedAt = DateTime.Now;
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = fineAmount > 0
                ? $"บันทึกค่าประเมิน {request.ProductName} {fineAmount:N2} ฿ แล้ว ยอดมัดจำส่วนที่เหลือจะไปแสดงในรายการคืนมัดจำ"
                : $"บันทึกยกเว้นค่าปรับ {request.ProductName} แล้ว";
            return RedirectToAction(nameof(DamagesAndRefunds), new { tab = "damages" });
        }

        [HttpGet]
        public async Task<IActionResult> DamageEvidence(uint? damageId, uint? requestId)
        {
            await EnsureAccountingRoleAndUserAsync();
            string? photoName = null;
            if (damageId.HasValue)
            {
                photoName = await _db.EquipmentDamages
                    .Where(d => d.DamageId == damageId.Value)
                    .Select(d => d.PhotoUrl)
                    .FirstOrDefaultAsync();
            }
            else if (requestId.HasValue)
            {
                photoName = await _db.RentalDepositRefundRequests
                    .Where(r => r.RequestId == requestId.Value)
                    .Select(r => r.DamagePhotoUrl)
                    .FirstOrDefaultAsync();
            }

            if (string.IsNullOrWhiteSpace(photoName)) return NotFound();
            var filePath = UploadStorage.GetDamagePath(_env, photoName);
            var contentType = Path.GetExtension(filePath).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => null
            };
            if (contentType == null || !System.IO.File.Exists(filePath)) return NotFound();
            Response.Headers.CacheControl = "no-store, private";
            return PhysicalFile(filePath, contentType);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessRentalDepositRefund(uint requestId, string refundMethod, string? refundReference)
        {
            await EnsureAccountingRoleAndUserAsync();
            var request = await _db.RentalDepositRefundRequests.FirstOrDefaultAsync(r => r.RequestId == requestId);
            if (request == null || request.Status != "PENDING")
            {
                TempData["ErrorMessage"] = "ไม่พบคำขอ หรือคำขอนี้ถูกดำเนินการแล้ว";
                return RedirectToAction(nameof(DamagesAndRefunds), new { tab = "depositRefunds" });
            }

            decimal fineAmount = 0m;
            List<EquipmentDamage> bookingDamages = new();
            if (!string.IsNullOrWhiteSpace(request.DamageDescription))
            {
                if (request.SaleId.HasValue)
                {
                    if (request.DamageAssessmentStatus is not ("ASSESSED" or "WAIVED"))
                    {
                        TempData["ErrorMessage"] = "กรุณาให้บัญชีประเมินค่าปรับก่อนดำเนินการคืนมัดจำ";
                        return RedirectToAction(nameof(DamagesAndRefunds), new { tab = "damages" });
                    }
                    fineAmount = request.DamageAssessmentStatus == "WAIVED" ? 0m : request.FineAmount;
                }
                else if (request.BookingItemId.HasValue)
                {
                    bookingDamages = await _db.EquipmentDamages
                        .Where(d => d.BookingItemId == request.BookingItemId.Value)
                        .ToListAsync();
                    if (bookingDamages.Any(d => d.Status == "REPORTED"))
                    {
                        TempData["ErrorMessage"] = "กรุณาให้บัญชีกำหนดค่าปรับในแท็บอุปกรณ์ชำรุดก่อน แล้วจึงดำเนินการคืนมัดจำ";
                        return RedirectToAction(nameof(DamagesAndRefunds), new { tab = "damages" });
                    }
                    fineAmount = bookingDamages.Where(d => d.Status is "CHARGED" or "PAID").Sum(d => d.FineAmount);
                }
                else
                {
                    TempData["ErrorMessage"] = "ไม่พบรายการประเมินค่าปรับที่เชื่อมกับคำขอคืนมัดจำนี้";
                    return RedirectToAction(nameof(DamagesAndRefunds), new { tab = "depositRefunds" });
                }
            }

            refundMethod = (refundMethod ?? string.Empty).Trim().ToUpperInvariant();
            fineAmount = Math.Max(0m, fineAmount);
            var depositDeduction = Math.Min(request.DepositAmount, fineAmount);
            var expectedRefund = Math.Max(0m, request.DepositAmount - depositDeduction);
            bool requiresTransfer = expectedRefund > 0;
            if (requiresTransfer && refundMethod is not ("PROMPTPAY" or "BANK"))
            {
                TempData["ErrorMessage"] = "ตรวจสอบยอดค่าปรับและช่องทางคืนเงินอีกครั้ง";
                return RedirectToAction(nameof(DamagesAndRefunds), new { tab = "depositRefunds" });
            }
            if (requiresTransfer && string.IsNullOrWhiteSpace(refundReference))
            {
                TempData["ErrorMessage"] = "กรุณาระบุเลขอ้างอิงการโอนหรือหมายเหตุการคืนเงิน";
                return RedirectToAction(nameof(DamagesAndRefunds), new { tab = "depositRefunds" });
            }

            var accountantId = await GetCurrentAccountantIdAsync();
            foreach (var damage in bookingDamages.Where(d => d.Status == "CHARGED" && fineAmount <= request.DepositAmount))
            {
                damage.Status = "PAID";
                damage.ProcessedBy = accountantId;
                damage.ProcessedAt = DateTime.Now;
            }
            request.FineAmount = fineAmount;
            request.RefundedAmount = expectedRefund;
            request.RefundMethod = requiresTransfer ? refundMethod : "DEPOSIT_OFFSET";
            request.RefundReference = requiresTransfer
                ? refundReference!.Trim()[..Math.Min(refundReference.Trim().Length, 100)]
                : $"หักค่าปรับจากมัดจำ {depositDeduction:N2} ฿";
            request.Status = "COMPLETED";
            request.ProcessedBy = accountantId;
            request.ProcessedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = $"บันทึกการคืนมัดจำ {request.ReceiptCode}: หักจากมัดจำ {depositDeduction:N2} ฿ คืน {expectedRefund:N2} ฿" +
                (fineAmount > request.DepositAmount ? $" และยังมีค่าปรับคงเหลือ {fineAmount - request.DepositAmount:N2} ฿" : string.Empty);
            return RedirectToAction(nameof(DamagesAndRefunds), new { tab = "depositRefunds" });
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
            string selectedDateStr = string.IsNullOrWhiteSpace(date) ? DateFormatHelper.ToIsoDate(DateTime.Today) : date.Trim();

            ViewBag.SelectedYear = currentYear;
            ViewBag.SelectedMonth = currentMonth;
            ViewBag.SelectedDate = selectedDateStr;

            if (currentType == "daily")
            {
                DateOnly targetDate = DateFormatHelper.TryParseIsoDate(selectedDateStr, out var pd) ? pd : DateOnly.FromDateTime(DateTime.Today);

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
                    .Concat(monthlyPosSales.Where(s => s.PaidAt.HasValue).Select(s => new { Day = s.PaidAt!.Value.Day, Court = 0m, Sale = s.SaleAmount, Rent = s.RentalAmount, Net = s.NetTotal, Count = 1 }))
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
            string? uidStr = HttpContext.Session.GetString("UserId");
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
