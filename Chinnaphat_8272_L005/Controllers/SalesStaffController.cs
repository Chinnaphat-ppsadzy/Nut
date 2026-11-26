using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Chinnaphat_8272_L005.Models;
using Chinnaphat_8272_L005.Helpers;
using Chinnaphat_8272_L005.Database;
using Microsoft.AspNetCore.Hosting;
using Chinnaphat_8272_L005.ViewModels;

namespace Chinnaphat_8272_L005.Controllers
{
    [RequireRole("SALES_STAFF")]
    public class SalesStaffController : Controller
    {
        private readonly Csi402dbContext _db;
        private readonly IWebHostEnvironment _env;

        public SalesStaffController(Csi402dbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // ============================================================
        // DASHBOARD (Sales & Rental Staff)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "แดชบอร์ดฝ่ายซื้อ-เช่าอุปกรณ์";
            await EnsureSalesStaffRoleAndProductsAsync();

            var products = await _db.Products.Include(p => p.Category).ToListAsync();
            ViewBag.TotalProducts = products.Count;
            ViewBag.SaleProductsCount = products.Count(p => p.ProductType == "SALE");
            ViewBag.RentProductsCount = products.Count(p => p.ProductType == "RENT");
            ViewBag.LowStockCount = products.Count(p => p.StockQty <= 5);

            return View(products);
        }

        // ============================================================
        // SALES POS — หน้าขายสินค้าและให้เช่าอุปกรณ์ (POS หน้าร้าน)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Sales(string? category)
        {
            ViewData["Title"] = "ขายสินค้าและเช่าอุปกรณ์ (POS หน้าร้าน)";
            await EnsureSalesStaffRoleAndProductsAsync();

            var query = _db.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive == true || p.IsActive == null);

            if (!string.IsNullOrWhiteSpace(category) && category != "ALL")
            {
                query = query.Where(p => p.Category.CategoryCode == category || p.ProductType == category);
            }

            var products = await query
                .OrderBy(p => p.ProductType)
                .ThenBy(p => p.CategoryId)
                .ThenBy(p => p.ProductName)
                .ToListAsync();

            ViewBag.Categories = await _db.ProductCategories.OrderBy(c => c.ProductType).ToListAsync();
            ViewBag.ActiveCategory = category ?? "ALL";

            // ดึงโปรโมชั่นที่เปิดใช้งานอยู่เพื่อนำไปคำนวณส่วนลดหน้าร้าน
            var activePromos = await _db.Promotions
                .Where(p => p.IsActive == true)
                .OrderBy(p => p.Priority)
                .ToListAsync();
            ViewBag.Promotions = activePromos;

            return View(products);
        }

        // ============================================================
        // CHECKOUT POS — บันทึกการขายและตัดสต็อกในฐานข้อมูล
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(
            string customerName,
            string customerPhone,
            string paymentMethod,
            string itemsJson,
            decimal subtotal,
            decimal discount,
            decimal netTotal,
            string? appliedPromo,
            IFormFile? slipFile)
        {
            if (string.IsNullOrWhiteSpace(itemsJson) || itemsJson == "[]")
            {
                TempData["ErrorMessage"] = "ไม่มีรายการสินค้าในบิล ไม่สามารถบันทึกการขายได้";
                return RedirectToAction(nameof(Sales));
            }

            try
            {
                customerPhone = (customerPhone ?? string.Empty).Trim();
                if (customerPhone.Length > 0 &&
                    (customerPhone.Length != 10 || customerPhone.Any(ch => ch < '0' || ch > '9')))
                {
                    TempData["ErrorMessage"] = "เบอร์โทรศัพท์ต้องเป็นตัวเลขให้ครบ 10 หลัก หรือเว้นว่างไว้";
                    return RedirectToAction(nameof(Sales));
                }

                paymentMethod = (paymentMethod ?? string.Empty).Trim().ToUpperInvariant();
                if (paymentMethod != "QR" || subtotal < 0 || discount < 0 || netTotal < 0)
                {
                    TempData["ErrorMessage"] = "ระบบรับชำระผ่าน QR เท่านั้น";
                    return RedirectToAction(nameof(Sales));
                }
                if (!IsValidSlip(slipFile))
                {
                    TempData["ErrorMessage"] = "กรุณาแนบสลิป JPG, PNG หรือ WEBP ขนาดไม่เกิน 5 MB";
                    return RedirectToAction(nameof(Sales));
                }

                // Parse items JSON
                var items = System.Text.Json.JsonSerializer.Deserialize<List<PosCartItemDto>>(itemsJson);
                if (items == null || !items.Any() || items.Any(i => i.ProductId <= 0 || i.Qty <= 0 || i.Price < 0))
                {
                    TempData["ErrorMessage"] = "ข้อมูลสินค้าไม่ถูกต้อง";
                    return RedirectToAction(nameof(Sales));
                }

                // ตัดสต็อกสินค้าในฐานข้อมูล
                var updatedItemsSummary = new List<string>();
                decimal posSaleAmount = 0m;
                decimal posRentalAmount = 0m;
                decimal authoritativeSubtotal = 0m;
                decimal authoritativeDeposit = 0m;
                var normalizedItems = new List<PosCartItemDto>();
                foreach (var item in items)
                {
                    var product = await _db.Products.FindAsync((uint)item.ProductId);
                    if (product == null || (product.IsActive != true && product.IsActive != null))
                    {
                        TempData["ErrorMessage"] = "พบสินค้าที่ไม่มีอยู่หรือปิดใช้งานแล้ว กรุณาตรวจสอบตะกร้าอีกครั้ง";
                        return RedirectToAction(nameof(Sales));
                    }
                    if (item.Qty > product.StockQty)
                    {
                        TempData["ErrorMessage"] = $"สินค้า {product.ProductName} มีสต็อกไม่พอ (เหลือ {product.StockQty})";
                        return RedirectToAction(nameof(Sales));
                    }

                    product.StockQty -= item.Qty;
                    product.UpdatedAt = DateTime.Now;
                    var lineTotal = product.Price * item.Qty;
                    authoritativeSubtotal += lineTotal;
                    var deposit = product.ProductType == "RENT" ? product.Deposit : 0m;
                    authoritativeDeposit += deposit * item.Qty;
                    updatedItemsSummary.Add($"{product.ProductName} x{item.Qty} ({product.Price:N0} ฿)");
                    if (product.ProductType == "RENT") posRentalAmount += lineTotal;
                    else posSaleAmount += lineTotal;
                    normalizedItems.Add(new PosCartItemDto { ProductId = (int)product.ProductId, ProductName = product.ProductName, ProductType = product.ProductType, Price = product.Price, Deposit = deposit, Qty = item.Qty, Unit = product.Unit });
                }

                if (discount > authoritativeSubtotal)
                {
                    TempData["ErrorMessage"] = "ส่วนลดมากกว่ายอดสินค้า ไม่สามารถบันทึกการขายได้";
                    return RedirectToAction(nameof(Sales));
                }

                var now = DateTime.Now;
                string receiptSuffix = Guid.NewGuid().ToString("N")[..8];
                string receiptCode = $"POS-{now:yyyyMMddHHmmss}-{receiptSuffix}";
                uint.TryParse(HttpContext.Session.GetString("UserId"), out var salesStaffId);
                decimal paidTotal = authoritativeSubtotal - discount + authoritativeDeposit;
                var slipDirectory = UploadStorage.GetSlipDirectory(_env);
                Directory.CreateDirectory(slipDirectory);
                var slipName = $"pos_{receiptSuffix}{Path.GetExtension(slipFile!.FileName).ToLowerInvariant()}";
                var slipPath = Path.Combine(slipDirectory, slipName);
                await using (var stream = new FileStream(slipPath, FileMode.CreateNew)) await slipFile.CopyToAsync(stream);
                _db.PosSaleTransactions.Add(new PosSaleTransaction
                {
                    ReceiptCode = receiptCode,
                    CustomerName = string.IsNullOrWhiteSpace(customerName) ? "ลูกค้าทั่วไป (Walk-in)" : customerName.Trim()[..Math.Min(customerName.Trim().Length, 150)],
                    CustomerPhone = string.IsNullOrWhiteSpace(customerPhone) ? null : customerPhone,
                    ItemsSummary = string.Join(" · ", updatedItemsSummary),
                    ItemsJson = System.Text.Json.JsonSerializer.Serialize(normalizedItems),
                    SaleAmount = posSaleAmount,
                    RentalAmount = posRentalAmount,
                    DiscountAmount = discount,
                    NetTotal = paidTotal,
                    PaymentMethod = paymentMethod,
                    PaymentStatus = "PENDING_VERIFY",
                    PaidAt = null,
                    SlipImageUrl = $"/uploads/slips/{slipName}",
                    CreatedBy = salesStaffId,
                    CreatedAt = now
                });
                await _db.SaveChangesAsync();

                TempData["SuccessMessage"] = $"ส่งรายการ {receiptCode} ยอด {paidTotal:N2} บาทพร้อมสลิปให้ฝ่ายบัญชีตรวจสอบแล้ว (สำรองสต็อกไว้ระหว่างรอตรวจ)";
                TempData["LastReceiptCode"] = receiptCode;
                TempData["LastCustomerName"] = string.IsNullOrWhiteSpace(customerName) ? "ลูกค้าทั่วไป (Walk-in)" : customerName;
                TempData["LastNetTotal"] = paidTotal.ToString("N0");
                TempData["LastPaymentMethod"] = "สแกนจ่าย PromptPay QR · รอตรวจสอบ";
                TempData["LastSummary"] = string.Join(", ", updatedItemsSummary);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "เกิดข้อผิดพลาดในการส่งรายการให้ฝ่ายบัญชี: " + ex.Message;
            }

            return RedirectToAction(nameof(Sales));
        }

        private static bool IsValidSlip(IFormFile? file)
        {
            if (file == null || file.Length <= 0 || file.Length > 5 * 1024 * 1024) return false;
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var contentType = file.ContentType.ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => contentType == "image/jpeg",
                ".png" => contentType == "image/png",
                ".webp" => contentType == "image/webp",
                _ => false
            };
        }

        // ============================================================
        // STOCK MANAGEMENT — หน้าจัดการสต็อกเหมือนในหน้า Admin/Product
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Stock()
        {
            ViewData["Title"] = "จัดการสต็อกและอุปกรณ์ (ฝ่ายซื้อ-เช่า)";
            await EnsureSalesStaffRoleAndProductsAsync();

            var products = await _db.Products
                .Include(p => p.Category)
                .OrderBy(p => p.ProductType)
                .ThenBy(p => p.CategoryId)
                .ThenBy(p => p.ProductId)
                .ToListAsync();

            ViewBag.Categories = await _db.ProductCategories
                .OrderBy(c => c.ProductType)
                .ThenBy(c => c.CategoryName)
                .ToListAsync();

            return View(products);
        }

        // POST: อัปเดตสต็อกสินค้าโดยฝ่ายซื้อ-เช่า
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStock(uint productId, int newStockQty, string? reason)
        {
            var product = await _db.Products.FindAsync(productId);
            if (product != null)
            {
                int oldQty = product.StockQty;
                product.StockQty = Math.Max(0, newStockQty);
                product.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync();

                TempData["SuccessMessage"] = $"✓ อัปเดตสต็อก {product.ProductName} จาก {oldQty} เป็น {product.StockQty} {product.Unit} เรียบร้อยแล้ว (สาเหตุ: {reason ?? "ตรวจนับประจำวัน"})";
            }
            return RedirectToAction(nameof(Stock));
        }

        // ============================================================
        // RENTALS & RETURNS — หน้าส่งมอบและตรวจรับคืนอุปกรณ์เช่า
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Rentals()
        {
            ViewData["Title"] = "ส่งมอบสินค้าและจัดการอุปกรณ์เช่า";
            await EnsureSalesStaffRoleAndProductsAsync();
            await WalkInPaymentSchemaInitializer.EnsureRentalDepositRefundTableAsync(_db);

            ViewBag.PendingSaleItems = await _db.BookingItems
                .Include(i => i.Product)
                .Include(i => i.Booking).ThenInclude(b => b.User)
                .Where(i => i.ItemType == "SALE" && i.ItemStatus == "RESERVED"
                    && (i.Booking.Status == "CONFIRMED" || i.Booking.Status == "CHECKED_IN" || i.Booking.Status == "COMPLETED")
                    && i.Booking.Payments.Any(p => p.Status == "APPROVED" || p.Status == "PAID"))
                .OrderBy(i => i.Booking.CreatedAt).ThenBy(i => i.BookingItemId)
                .ToListAsync();

            var rentalProducts = await _db.Products
                .Include(p => p.Category)
                .Where(p => p.ProductType == "RENT")
                .ToListAsync();

            // ดึงรายการความเสียหายที่เคยแจ้งไว้
            var damages = await _db.EquipmentDamages
                .Include(d => d.BookingItem).ThenInclude(i => i.Product)
                .Include(d => d.BookingItem).ThenInclude(i => i.Booking).ThenInclude(b => b.User)
                .Include(d => d.ReportedByNavigation)
                .OrderByDescending(d => d.CreatedAt)
                .Take(20)
                .ToListAsync();
            ViewBag.Damages = damages;

            ViewBag.ActiveRentalItems = await _db.BookingItems
                .Include(i => i.Product)
                .Include(i => i.Booking).ThenInclude(b => b.User)
                .Where(i => i.ItemType == "RENT" && (i.ItemStatus == "RESERVED" || i.ItemStatus == "HANDED_OVER") &&
                    (i.Booking.Status == "CONFIRMED" || i.Booking.Status == "CHECKED_IN" || i.Booking.Status == "COMPLETED"))
                .OrderBy(i => i.Booking.PlayDate).ThenBy(i => i.BookingItemId)
                .ToListAsync();

            var posRentals = new List<PosRentalLineViewModel>();
            var paidPosSales = await _db.PosSaleTransactions
                .Where(s => s.PaymentStatus == "PAID")
                .OrderByDescending(s => s.PaidAt)
                .Take(100)
                .ToListAsync();
            var posSaleIds = paidPosSales.Select(s => s.SaleId).ToList();
            var registeredPosReturns = await _db.RentalDepositRefundRequests
                .Where(r => r.SaleId.HasValue && posSaleIds.Contains(r.SaleId.Value) && r.Status != "REJECTED")
                .ToListAsync();
            foreach (var sale in paidPosSales)
            {
                if (string.IsNullOrWhiteSpace(sale.ItemsJson)) continue;
                var items = System.Text.Json.JsonSerializer.Deserialize<List<PosCartItemDto>>(sale.ItemsJson) ?? new();
                foreach (var item in items.Where(i => i.ProductType == "RENT"))
                {
                    var pendingQty = item.DepositRefunds.Where(r => r.Status == "PENDING").Sum(r => r.Quantity);
                    var pendingAmount = item.DepositRefunds.Where(r => r.Status == "PENDING").Sum(r => r.Amount) + registeredPosReturns
                        .Where(r => r.SaleId == sale.SaleId && r.ProductId == (uint)item.ProductId && r.Status == "PENDING")
                        .Sum(r => r.DepositAmount);
                    var returnedQty = item.DepositRefunds.Sum(r => r.Quantity) + registeredPosReturns
                        .Where(r => r.SaleId == sale.SaleId && r.ProductId == (uint)item.ProductId)
                        .Sum(r => r.Quantity);
                    var remainingQty = Math.Max(0, item.Qty - returnedQty);
                    if (remainingQty > 0 || pendingQty > 0)
                        posRentals.Add(new PosRentalLineViewModel { Sale = sale, Item = item, RemainingQty = remainingQty, PendingRefundAmount = pendingAmount });
                }
            }
            ViewBag.PosRentalItems = posRentals;

            return View(rentalProducts);
        }

        [HttpGet]
        public async Task<IActionResult> BookingRefunds()
        {
            await EnsureSalesStaffRoleAndProductsAsync();
            await BookingRefundSchemaInitializer.EnsureAsync(_db);
            ViewBag.RefundRequests = await _db.BookingRefundRequests
                .Where(r => r.BookingItemId.HasValue && r.Status == "PENDING_SALES")
                .OrderBy(r => r.CreatedAt).ToListAsync();
            var allProductRefundRequests = await _db.BookingRefundRequests
                .Where(r => r.BookingItemId.HasValue || r.SaleId.HasValue)
                .OrderByDescending(r => r.CreatedAt).ToListAsync();

            var trackingItems = allProductRefundRequests.Select(r => new SalesRefundTrackingItem
            {
                RequestType = "คืนค่าสินค้า",
                Source = r.SaleId.HasValue ? "Walk-in / POS" : "ลูกค้าสมาชิก / ออนไลน์",
                ReferenceCode = r.ReferenceCode,
                CustomerName = r.CustomerName,
                ProductName = r.ProductName ?? "สินค้า",
                Quantity = checked((int)(r.ProductQuantity ?? 0)),
                Amount = r.Amount,
                AmountLabel = "ยอดขอคืน",
                Reason = r.Reason,
                Status = r.Status,
                StatusText = r.Status switch
                {
                    "PENDING_SALES" => "รอฝ่ายขายตรวจรับ",
                    "PENDING_ACCOUNTING" => "ส่งบัญชีแล้ว · รอคืนเงินจริง",
                    "COMPLETED" => "บัญชีบันทึกคืนเงินแล้ว",
                    "REJECTED" => "ปฏิเสธคำขอ",
                    _ => r.Status
                },
                StatusClass = r.Status switch
                {
                    "COMPLETED" => "completed",
                    "REJECTED" => "rejected",
                    "PENDING_ACCOUNTING" => "accounting",
                    _ => "sales"
                },
                SalesNote = r.SalesNote,
                AccountingNote = r.AccountingNote,
                RefundMethod = r.RefundMethod,
                RefundReference = r.RefundReference,
                CreatedAt = r.CreatedAt,
                ProcessedAt = r.ProcessedAt
            }).ToList();

            ViewBag.SalesRefundTrackingItems = trackingItems.OrderByDescending(r => r.CreatedAt).Take(200).ToList();
            ViewBag.PendingProductSalesCount = allProductRefundRequests.Count(r => r.Status == "PENDING_SALES");
            ViewBag.PendingAccountingCount = allProductRefundRequests.Count(r => r.Status == "PENDING_ACCOUNTING");
            ViewBag.CompletedRefundCount = allProductRefundRequests.Count(r => r.Status == "COMPLETED");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewBookingRefund(uint requestId, bool itemReturned, string? salesNote)
        {
            await BookingRefundSchemaInitializer.EnsureAsync(_db);
            var request = await _db.BookingRefundRequests.FirstOrDefaultAsync(r => r.RequestId == requestId && r.Status == "PENDING_SALES");
            if (request == null) return RedirectToAction(nameof(BookingRefunds));
            var note = (salesNote ?? string.Empty).Trim();
            if (note.Length > 500) { TempData["ErrorMessage"] = "บันทึกผลตรวจได้ไม่เกิน 500 ตัวอักษร"; return RedirectToAction(nameof(BookingRefunds)); }
            if (!itemReturned)
            {
                if (note.Length < 3) { TempData["ErrorMessage"] = "ระบุเหตุผลเมื่อยังไม่ได้รับสินค้าคืน"; return RedirectToAction(nameof(BookingRefunds)); }
                request.Status = "REJECTED";
            }
            else
            {
                var item = request.BookingItemId.HasValue
                    ? await _db.BookingItems.Include(i => i.Product).ThenInclude(p => p.Category)
                        .FirstOrDefaultAsync(i => i.BookingItemId == request.BookingItemId.Value)
                    : null;
                if (!RefundEligibility.IsRefundableEquipment(item) || item!.LineTotal != request.Amount)
                {
                    TempData["ErrorMessage"] = "ส่งต่อได้เฉพาะคำขอคืนอุปกรณ์กีฬาที่เข้าเงื่อนไข และยอดสินค้าต้องตรงกับบิล";
                    return RedirectToAction(nameof(BookingRefunds));
                }
                request.Status = "PENDING_ACCOUNTING";
            }
            request.SalesNote = note.Length > 0 ? note : "ฝ่ายขายตรวจรับสินค้าคืนแล้ว";
            request.SalesReviewedBy = await GetCurrentSalesStaffIdAsync();
            request.SalesReviewedAt = DateTime.Now;
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = request.Status == "PENDING_ACCOUNTING" ? "ยืนยันแล้ว ส่งต่อฝ่ายบัญชี" : "ปฏิเสธคำขอและแจ้งเหตุผลแล้ว";
            return RedirectToAction(nameof(BookingRefunds));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnPosRental(uint saleId, int productId, int quantity, string? damageDescription, IFormFile? damagePhoto, bool returnToBookingRefunds = false)
        {
            await WalkInPaymentSchemaInitializer.EnsureRentalDepositRefundTableAsync(_db);
            var returnAction = returnToBookingRefunds ? nameof(BookingRefunds) : nameof(Rentals);
            if (quantity <= 0)
            {
                TempData["ErrorMessage"] = "กรุณาระบุจำนวนอุปกรณ์ที่รับคืนให้ถูกต้อง";
                return RedirectToAction(returnAction);
            }

            var sale = await _db.PosSaleTransactions.FirstOrDefaultAsync(s => s.SaleId == saleId);
            if (sale == null || sale.PaymentStatus != "PAID" || string.IsNullOrWhiteSpace(sale.ItemsJson))
            {
                TempData["ErrorMessage"] = "ไม่พบรายการเช่าที่ชำระเงินแล้ว";
                return RedirectToAction(returnAction);
            }

            var items = System.Text.Json.JsonSerializer.Deserialize<List<PosCartItemDto>>(sale.ItemsJson) ?? new();
            var item = items.FirstOrDefault(i => i.ProductId == productId && i.ProductType == "RENT");
            var product = await _db.Products.FirstOrDefaultAsync(p => p.ProductId == (uint)productId && p.ProductType == "RENT");
            if (item == null || product == null)
            {
                TempData["ErrorMessage"] = "ไม่พบอุปกรณ์เช่าในใบเสร็จนี้";
                return RedirectToAction(returnAction);
            }

            var previousLegacyQty = item.DepositRefunds.Sum(r => r.Quantity);
            var previousRegisteredQty = await _db.RentalDepositRefundRequests
                .Where(r => r.SaleId == saleId && r.ProductId == (uint)productId && r.Status != "REJECTED")
                .SumAsync(r => r.Quantity);
            var alreadyRequested = previousLegacyQty + previousRegisteredQty;
            if (quantity > item.Qty - alreadyRequested)
            {
                TempData["ErrorMessage"] = "จำนวนคืนเกินจำนวนที่ยังไม่ได้คืน";
                return RedirectToAction(returnAction);
            }

            // ฝ่ายขายรับสินค้าและยืนยันสภาพ บัญชีเป็นผู้บันทึกการคืนเงินจริงภายหลัง
            uint staffId = await GetCurrentSalesStaffIdAsync();
            // แนบรูปอย่างเดียวก็ถือเป็นการแจ้งชำรุดได้ เพื่อไม่ให้คำขอถูกตีกลับก่อนส่งถึงบัญชี
            bool hasDamagePhoto = damagePhoto is { Length: > 0 };
            bool isDamaged = !string.IsNullOrWhiteSpace(damageDescription) || hasDamagePhoto;
            if (damagePhoto is { Length: > 0 } && !TryGetDamagePhotoExtension(damagePhoto, out _))
            {
                TempData["ErrorMessage"] = "รูปหลักฐานต้องเป็น JPG, PNG หรือ WEBP และมีขนาดไม่เกิน 5 MB";
                return RedirectToAction(returnAction);
            }

            string? damagePhotoName = null;
            if (damagePhoto is { Length: > 0 })
            {
                TryGetDamagePhotoExtension(damagePhoto, out var extension);
                damagePhotoName = $"damage_pos_{Guid.NewGuid():N}{extension}";
                var damageDirectory = UploadStorage.GetDamageDirectory(_env);
                Directory.CreateDirectory(damageDirectory);
                await using var stream = new FileStream(UploadStorage.GetDamagePath(_env, damagePhotoName), FileMode.CreateNew);
                await damagePhoto.CopyToAsync(stream);
            }
            _db.RentalDepositRefundRequests.Add(new RentalDepositRefundRequest
            {
                SaleId = sale.SaleId,
                ProductId = (uint)item.ProductId,
                ReceiptCode = sale.ReceiptCode,
                CustomerName = sale.CustomerName,
                CustomerPhone = sale.CustomerPhone,
                ProductName = item.ProductName,
                Quantity = quantity,
                DepositAmount = item.Deposit * quantity,
                DamageDescription = isDamaged
                    ? (string.IsNullOrWhiteSpace(damageDescription)
                        ? "แจ้งอุปกรณ์ชำรุดจากรูปหลักฐาน (ฝ่ายขายไม่ได้ระบุรายละเอียดเพิ่มเติม)"
                        : damageDescription.Trim()[..Math.Min(damageDescription.Trim().Length, 500)])
                    : null,
                DamagePhotoUrl = damagePhotoName,
                DamageAssessmentStatus = isDamaged ? "REPORTED" : null,
                Status = "PENDING",
                RequestedBy = staffId,
                CreatedAt = DateTime.Now
            });
            if (!isDamaged)
            {
                product.StockQty += quantity;
                product.UpdatedAt = DateTime.Now;
            }
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = isDamaged
                ? $"รับคืน {item.ProductName} และส่งรายละเอียดชำรุดให้ฝ่ายบัญชีประเมินก่อนคืนมัดจำ"
                : $"รับคืน {item.ProductName} จำนวน {quantity} แล้ว ส่งยอดมัดจำ {item.Deposit * quantity:N2} ฿ ให้ฝ่ายบัญชีอนุมัติคืน";
            return RedirectToAction(returnAction);
        }

        // POST: บันทึกความเสียหายของอุปกรณ์เช่า (ส่งต่อให้ฝ่ายบัญชีคิดค่าปรับ)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRentalHandedOver(uint bookingItemId)
        {
            var item = await _db.BookingItems.Include(i => i.Booking).FirstOrDefaultAsync(i => i.BookingItemId == bookingItemId);
            if (item == null || item.ItemType != "RENT" || item.ItemStatus != "RESERVED" || item.Booking.Status is "CANCELLED" or "REJECTED" or "EXPIRED")
            {
                TempData["ErrorMessage"] = "ไม่พบรายการเช่าที่พร้อมส่งมอบ";
                return RedirectToAction(nameof(Rentals));
            }

            var staffId = await GetCurrentSalesStaffIdAsync();
            item.ItemStatus = "HANDED_OVER";
            item.HandedOverAt = DateTime.Now;
            item.HandedOverBy = staffId;
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "บันทึกการส่งมอบอุปกรณ์ให้ลูกค้าแล้ว";
            return RedirectToAction(nameof(Rentals));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkSaleItemDelivered(uint bookingItemId)
        {
            var item = await _db.BookingItems
                .Include(i => i.Product)
                .Include(i => i.Booking).ThenInclude(b => b.Payments)
                .FirstOrDefaultAsync(i => i.BookingItemId == bookingItemId);
            if (item == null || item.ItemType != "SALE" || item.ItemStatus != "RESERVED"
                || item.Booking.Status is not ("CONFIRMED" or "CHECKED_IN" or "COMPLETED")
                || !item.Booking.Payments.Any(p => p.Status is "APPROVED" or "PAID"))
            {
                TempData["ErrorMessage"] = "ไม่พบสินค้าที่พร้อมส่งมอบ ต้องเป็นรายการขายที่บัญชียืนยันการชำระแล้ว";
                return RedirectToAction(nameof(Rentals));
            }

            // The database enum supports HANDED_OVER (but not DELIVERED); this is
            // also the status used by the customer refund eligibility check.
            item.ItemStatus = "HANDED_OVER";
            item.HandedOverAt = DateTime.Now;
            item.HandedOverBy = await GetCurrentSalesStaffIdAsync();
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = $"บันทึกส่งมอบ {item.Product?.ProductName ?? "สินค้า"} ให้ลูกค้าแล้ว";
            return RedirectToAction(nameof(Rentals));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRentalReturned(uint bookingItemId)
        {
            var item = await _db.BookingItems.Include(i => i.Product).Include(i => i.Booking).ThenInclude(b => b.User)
                .FirstOrDefaultAsync(i => i.BookingItemId == bookingItemId);
            if (item == null || item.ItemType != "RENT" || item.ItemStatus != "HANDED_OVER")
            {
                TempData["ErrorMessage"] = "รายการนี้ยังไม่ถูกส่งมอบ หรือรับคืนไปแล้ว";
                return RedirectToAction(nameof(Rentals));
            }

            uint staffId = await GetCurrentSalesStaffIdAsync();
            item.ItemStatus = "RETURNED";
            item.ReturnedAt = DateTime.Now;
            item.ReturnedBy = staffId;
            item.Product.StockQty += checked((int)item.Quantity);
            item.Product.UpdatedAt = DateTime.Now;
            _db.RentalDepositRefundRequests.Add(new RentalDepositRefundRequest
            {
                BookingItemId = item.BookingItemId,
                ProductId = item.ProductId,
                ReceiptCode = item.Booking.BookingCode,
                CustomerName = item.Booking.User?.FullName ?? "ลูกค้าสมาชิก",
                CustomerPhone = item.Booking.User?.Phone,
                ProductName = item.Product.ProductName,
                Quantity = checked((int)item.Quantity),
                DepositAmount = item.Product.Deposit * item.Quantity,
                Status = "PENDING",
                RequestedBy = staffId,
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "ตรวจรับคืนอุปกรณ์สภาพปกติและคืนเข้าสต็อกแล้ว";
            return RedirectToAction(nameof(Rentals));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportDamage(uint bookingItemId, string description, IFormFile? damagePhoto)
        {
            var item = await _db.BookingItems.Include(i => i.Product).Include(i => i.Booking).ThenInclude(b => b.User)
                .FirstOrDefaultAsync(i => i.BookingItemId == bookingItemId);
            if (item == null || item.ItemType != "RENT" || item.ItemStatus != "HANDED_OVER")
            {
                TempData["ErrorMessage"] = "ต้องเลือกรายการอุปกรณ์ที่ส่งมอบแล้วและยังไม่รับคืน";
                return RedirectToAction(nameof(Rentals));
            }
            if (string.IsNullOrWhiteSpace(description))
            {
                TempData["ErrorMessage"] = "กรุณาระบุรายละเอียดความเสียหาย";
                return RedirectToAction(nameof(Rentals));
            }

            if (damagePhoto is { Length: > 0 } && !TryGetDamagePhotoExtension(damagePhoto, out _))
            {
                TempData["ErrorMessage"] = "รูปหลักฐานต้องเป็น JPG, PNG หรือ WEBP และมีขนาดไม่เกิน 5 MB";
                return RedirectToAction(nameof(Rentals));
            }

            var staffId = await GetCurrentSalesStaffIdAsync();
            string? damagePhotoName = null;
            if (damagePhoto is { Length: > 0 })
            {
                TryGetDamagePhotoExtension(damagePhoto, out var extension);
                damagePhotoName = $"damage_booking_{Guid.NewGuid():N}{extension}";
                var damageDirectory = UploadStorage.GetDamageDirectory(_env);
                Directory.CreateDirectory(damageDirectory);
                await using var stream = new FileStream(UploadStorage.GetDamagePath(_env, damagePhotoName), FileMode.CreateNew);
                await damagePhoto.CopyToAsync(stream);
            }
            _db.EquipmentDamages.Add(new EquipmentDamage
            {
                BookingItemId = item.BookingItemId,
                Description = description.Trim()[..Math.Min(description.Trim().Length, 500)],
                PhotoUrl = damagePhotoName,
                FineAmount = 0m,
                Status = "REPORTED",
                ReportedBy = staffId,
                CreatedAt = DateTime.Now
            });
            item.ItemStatus = "DAMAGED";
            item.ReturnedAt = DateTime.Now;
            item.ReturnedBy = staffId;
            _db.RentalDepositRefundRequests.Add(new RentalDepositRefundRequest
            {
                BookingItemId = item.BookingItemId,
                ProductId = item.ProductId,
                ReceiptCode = item.Booking.BookingCode,
                CustomerName = item.Booking.User?.FullName ?? "ลูกค้าสมาชิก",
                CustomerPhone = item.Booking.User?.Phone,
                ProductName = item.Product.ProductName,
                Quantity = checked((int)item.Quantity),
                DepositAmount = item.Product.Deposit * item.Quantity,
                DamageDescription = description.Trim()[..Math.Min(description.Trim().Length, 500)],
                DamagePhotoUrl = damagePhotoName,
                DamageAssessmentStatus = "REPORTED",
                Status = "PENDING",
                RequestedBy = staffId,
                CreatedAt = DateTime.Now
            });
            // Do not return damaged items to available stock; finance first determines the damage charge.
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = $"ส่งรายงานความเสียหายของ {item.Product.ProductName} ให้ฝ่ายบัญชีตรวจสอบแล้ว";
            return RedirectToAction(nameof(Rentals));
        }

        [HttpGet]
        public async Task<IActionResult> DamageEvidence(uint damageId)
        {
            var photoName = await _db.EquipmentDamages
                .Where(d => d.DamageId == damageId)
                .Select(d => d.PhotoUrl)
                .FirstOrDefaultAsync();
            if (string.IsNullOrWhiteSpace(photoName)) return NotFound();

            var filePath = UploadStorage.GetDamagePath(_env, photoName);
            var contentType = GetDamagePhotoContentType(filePath);
            if (contentType == null || !System.IO.File.Exists(filePath)) return NotFound();
            Response.Headers.CacheControl = "no-store, private";
            return PhysicalFile(filePath, contentType);
        }

        private static bool TryGetDamagePhotoExtension(IFormFile file, out string extension)
        {
            extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (file.Length <= 0 || file.Length > 5 * 1024 * 1024) return false;
            return extension switch
            {
                ".jpg" or ".jpeg" => file.ContentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase),
                ".png" => file.ContentType.Equals("image/png", StringComparison.OrdinalIgnoreCase),
                ".webp" => file.ContentType.Equals("image/webp", StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }

        private static string? GetDamagePhotoContentType(string filePath) => Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => null
        };

        private async Task<uint> GetCurrentSalesStaffIdAsync()
        {
            var rawId = HttpContext.Session.GetString("UserId");
            if (!uint.TryParse(rawId, out var userId)) throw new InvalidOperationException("ไม่พบผู้ใช้ที่เข้าสู่ระบบ กรุณาเข้าสู่ระบบใหม่");
            var isSalesStaff = await _db.Users.AnyAsync(u => u.UserId == userId && u.Role.RoleCode == "SALES_STAFF");
            if (!isSalesStaff) throw new InvalidOperationException("ผู้ใช้นี้ไม่มีสิทธิ์บันทึกรายการอุปกรณ์เช่า");
            return userId;
        }

        // ============================================================
        // HELPER: ตรวจสอบและเตรียม Role SALES_STAFF และสินค้าเริ่มต้น
        // ============================================================
        private async Task EnsureSalesStaffRoleAndProductsAsync()
        {
            // ตรวจสอบ Role SALES_STAFF ในตาราง roles
            var salesRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleCode == "SALES_STAFF");
            if (salesRole == null)
            {
                salesRole = new Role
                {
                    RoleCode = "SALES_STAFF",
                    RoleName = "ฝ่ายซื้อ-เช่าอุปกรณ์",
                    Description = "จัดการสต็อกหน้าร้าน ขายสินค้า ให้เช่าอุปกรณ์ ตรวจรับคืน และบันทึกอุปกรณ์ชำรุด"
                };
                _db.Roles.Add(salesRole);
                await _db.SaveChangesAsync();
            }

            // ตรวจสอบบัญชีผู้ใช้ตัวอย่างสำหรับ SALES_STAFF
            var salesUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == "sales@courtbook.com");
            if (salesUser == null)
            {
                salesUser = new User
                {
                    RoleId = salesRole.RoleId,
                    Email = "sales@courtbook.com",
                    PasswordHash = string.Empty,
                    FullName = "พนักงานฝ่ายซื้อ-เช่า",
                    Phone = "082-345-6789",
                    Status = "active",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                salesUser.PasswordHash = PasswordSecurity.Hash(salesUser, "123456");
                _db.Users.Add(salesUser);
                await _db.SaveChangesAsync();
            }

            // ตรวจสอบหมวดหมู่และสินค้า
            var catShuttle = await _db.ProductCategories.FirstOrDefaultAsync(c => c.CategoryCode == "SHUTTLECOCK");
            if (catShuttle == null)
            {
                catShuttle = new ProductCategory { CategoryCode = "SHUTTLECOCK", CategoryName = "ลูกแบดมินตัน", ProductType = "SALE" };
                _db.ProductCategories.Add(catShuttle);
            }

            var catRacket = await _db.ProductCategories.FirstOrDefaultAsync(c => c.CategoryCode == "RACKET");
            if (catRacket == null)
            {
                catRacket = new ProductCategory { CategoryCode = "RACKET", CategoryName = "ไม้แบดมินตัน", ProductType = "RENT" };
                _db.ProductCategories.Add(catRacket);
            }

            var catShoes = await _db.ProductCategories.FirstOrDefaultAsync(c => c.CategoryCode == "SHOES");
            if (catShoes == null)
            {
                catShoes = new ProductCategory { CategoryCode = "SHOES", CategoryName = "รองเท้าแบดมินตัน", ProductType = "RENT" };
                _db.ProductCategories.Add(catShoes);
            }

            var catBeverage = await _db.ProductCategories.FirstOrDefaultAsync(c => c.CategoryCode == "BEVERAGE");
            if (catBeverage == null)
            {
                catBeverage = new ProductCategory { CategoryCode = "BEVERAGE", CategoryName = "เครื่องดื่ม", ProductType = "SALE" };
                _db.ProductCategories.Add(catBeverage);
            }

            var catGrip = await _db.ProductCategories.FirstOrDefaultAsync(c => c.CategoryCode == "GRIP");
            if (catGrip == null)
            {
                catGrip = new ProductCategory { CategoryCode = "GRIP", CategoryName = "กริปและอุปกรณ์เสริม", ProductType = "SALE" };
                _db.ProductCategories.Add(catGrip);
            }

            await _db.SaveChangesAsync();

            // สินค้าเริ่มต้น
            if (!await _db.Products.AnyAsync())
            {
                var initialProducts = new List<Product>
                {
                    new() { Sku = "SHT-AS30", ProductName = "ลูกแบดมินตัน Yonex Aerosensa 30 (หลอด 12 ลูก)", CategoryId = catShuttle.CategoryId, ProductType = "SALE", Price = 850, Deposit = 0, Unit = "หลอด", StockQty = 50, IsActive = true, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                    new() { Sku = "SHT-RSL1", ProductName = "ลูกแบดมินตัน RSL Tourney No.1 (หลอด 12 ลูก)", CategoryId = catShuttle.CategoryId, ProductType = "SALE", Price = 720, Deposit = 0, Unit = "หลอด", StockQty = 40, IsActive = true, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                    new() { Sku = "RKT-AX88D", ProductName = "ไม้แบดมินตัน Yonex Astrox 88D Pro", CategoryId = catRacket.CategoryId, ProductType = "RENT", Price = 80, Deposit = 200, Unit = "ไม้", StockQty = 15, IsActive = true, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                    new() { Sku = "RKT-TK9900", ProductName = "ไม้แบดมินตัน Victor Thruster K 9900", CategoryId = catRacket.CategoryId, ProductType = "RENT", Price = 70, Deposit = 200, Unit = "ไม้", StockQty = 15, IsActive = true, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                    new() { Sku = "SHS-PC65Z", ProductName = "รองเท้าแบดมินตัน Yonex Power Cushion 65Z", CategoryId = catShoes.CategoryId, ProductType = "RENT", Price = 50, Deposit = 100, Unit = "คู่", StockQty = 20, IsActive = true, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                    new() { Sku = "BEV-SINGHA", ProductName = "น้ำดื่มสิงห์ 600 ml", CategoryId = catBeverage.CategoryId, ProductType = "SALE", Price = 15, Deposit = 0, Unit = "ขวด", StockQty = 100, IsActive = true, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                    new() { Sku = "BEV-POCARI", ProductName = "เครื่องดื่มเกลือแร่ Pocari Sweat 500 ml", CategoryId = catBeverage.CategoryId, ProductType = "SALE", Price = 30, Deposit = 0, Unit = "ขวด", StockQty = 50, IsActive = true, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                    new() { Sku = "GRP-AC102", ProductName = "กริปยางพันด้าม Yonex Super Grap (AC102EX)", CategoryId = catGrip.CategoryId, ProductType = "SALE", Price = 60, Deposit = 0, Unit = "ชิ้น", StockQty = 80, IsActive = true, CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now }
                };

                _db.Products.AddRange(initialProducts);
                await _db.SaveChangesAsync();
            }
        }
    }

    // DTO สำหรับรับข้อมูลรายการสินค้าจาก POS Terminal
    public class PosCartItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductType { get; set; } = "SALE";
        public decimal Price { get; set; }
        public decimal Deposit { get; set; }
        public int Qty { get; set; }
        public string Unit { get; set; } = "ชิ้น";
        public List<PosDepositRefundRequestDto> DepositRefunds { get; set; } = new();
    }

    public class PosDepositRefundRequestDto
    {
        public string RequestId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Amount { get; set; }
        public decimal? RefundedAmount { get; set; }
        public string? RefundMethod { get; set; }
        public string? RefundReference { get; set; }
        public string Status { get; set; } = "PENDING";
        public DateTime CreatedAt { get; set; }
        public uint? ProcessedBy { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }

    public class PosRentalLineViewModel
    {
        public PosSaleTransaction Sale { get; set; } = null!;
        public PosCartItemDto Item { get; set; } = null!;
        public int RemainingQty { get; set; }
        public decimal PendingRefundAmount { get; set; }
        public bool PendingDamageAssessment { get; set; }
    }

    public class PosDepositRefundViewModel
    {
        public PosSaleTransaction Sale { get; set; } = null!;
        public PosCartItemDto Item { get; set; } = null!;
        public PosDepositRefundRequestDto Request { get; set; } = null!;
    }
}
