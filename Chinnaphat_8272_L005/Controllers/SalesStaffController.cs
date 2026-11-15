using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Chinnaphat_8272_L005.Models;

namespace Chinnaphat_8272_L005.Controllers
{
    public class SalesStaffController : Controller
    {
        private readonly Csi402dbContext _db;

        public SalesStaffController(Csi402dbContext db)
        {
            _db = db;
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
            string? appliedPromo)
        {
            if (string.IsNullOrWhiteSpace(itemsJson) || itemsJson == "[]")
            {
                TempData["ErrorMessage"] = "ไม่มีรายการสินค้าในบิล ไม่สามารถบันทึกการขายได้";
                return RedirectToAction(nameof(Sales));
            }

            try
            {
                // Parse items JSON
                var items = System.Text.Json.JsonSerializer.Deserialize<List<PosCartItemDto>>(itemsJson);
                if (items == null || !items.Any())
                {
                    TempData["ErrorMessage"] = "ข้อมูลสินค้าไม่ถูกต้อง";
                    return RedirectToAction(nameof(Sales));
                }

                // ตัดสต็อกสินค้าในฐานข้อมูล
                var updatedItemsSummary = new List<string>();
                foreach (var item in items)
                {
                    var product = await _db.Products.FindAsync((uint)item.ProductId);
                    if (product != null)
                    {
                        // ตัดสต็อก
                        product.StockQty = Math.Max(0, product.StockQty - item.Qty);
                        product.UpdatedAt = DateTime.Now;

                        updatedItemsSummary.Add($"{product.ProductName} x{item.Qty} ({item.Price:N0} ฿)");
                    }
                }

                await _db.SaveChangesAsync();

                string receiptCode = "POS-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                TempData["SuccessMessage"] = $"✓ บันทึกการขายรหัส {receiptCode} สำเร็จ! ยอดสุทธิ {netTotal:N0} บาท (ตัดสต็อกสินค้าในฐานข้อมูลเรียบร้อยแล้ว)";
                TempData["LastReceiptCode"] = receiptCode;
                TempData["LastCustomerName"] = string.IsNullOrWhiteSpace(customerName) ? "ลูกค้าทั่วไป (Walk-in)" : customerName;
                TempData["LastNetTotal"] = netTotal.ToString("N0");
                TempData["LastPaymentMethod"] = paymentMethod == "QR" ? "โอนเงิน QR Code" : "เงินสด (Cash)";
                TempData["LastSummary"] = string.Join(", ", updatedItemsSummary);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "เกิดข้อผิดพลาดในการบันทึกการขาย: " + ex.Message;
            }

            return RedirectToAction(nameof(Sales));
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
            ViewData["Title"] = "ตรวจรับคืนและจัดการอุปกรณ์เช่า";
            await EnsureSalesStaffRoleAndProductsAsync();

            var rentalProducts = await _db.Products
                .Include(p => p.Category)
                .Where(p => p.ProductType == "RENT")
                .ToListAsync();

            // ดึงรายการความเสียหายที่เคยแจ้งไว้
            var damages = await _db.EquipmentDamages
                .Include(d => d.ReportedByNavigation)
                .OrderByDescending(d => d.CreatedAt)
                .Take(20)
                .ToListAsync();
            ViewBag.Damages = damages;

            return View(rentalProducts);
        }

        // POST: บันทึกความเสียหายของอุปกรณ์เช่า (ส่งต่อให้ฝ่ายบัญชีคิดค่าปรับ)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportDamage(
            uint productId,
            string equipmentName,
            string customerName,
            string description,
            decimal estimatedFine)
        {
            // ดึงผู้ใช้ที่เป็นฝ่ายซื้อ-เช่า หรือ fallback user แรก
            var reporter = await _db.Users.FirstOrDefaultAsync(u => u.Role.RoleCode == "SALES_STAFF")
                           ?? await _db.Users.FirstOrDefaultAsync();

            if (reporter != null)
            {
                var damage = new EquipmentDamage
                {
                    Description = $"[{equipmentName}] ลูกค้า: {customerName} - {description}",
                    FineAmount = estimatedFine > 0 ? estimatedFine : 0,
                    Status = "REPORTED",
                    ReportedBy = reporter.UserId,
                    CreatedAt = DateTime.Now
                };

                _db.EquipmentDamages.Add(damage);
                await _db.SaveChangesAsync();

                TempData["SuccessMessage"] = $"✓ บันทึกความเสียหายของอุปกรณ์ \"{equipmentName}\" เรียบร้อยแล้ว! ระบบได้ส่งเรื่องให้ฝ่ายบัญชีตรวจสอบและคิดค่าปรับ";
            }
            else
            {
                TempData["ErrorMessage"] = "ไม่สามารถบันทึกความเสียหายได้ เนื่องจากไม่พบข้อมูลผู้แจ้งในระบบ";
            }

            return RedirectToAction(nameof(Rentals));
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
                    PasswordHash = "123456",
                    FullName = "พนักงานฝ่ายซื้อ-เช่า",
                    Phone = "082-345-6789",
                    Status = "active",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
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
    }
}
