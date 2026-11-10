using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Chinnaphat_8272_L005.Models;

namespace Chinnaphat_8272_L005.Controllers
{
    public class AdminController : Controller
    {
        private readonly Csi402dbContext _db;

        public AdminController(Csi402dbContext db)
        {
            _db = db;
        }

        // ============================================================
        // ROUTE DASHBOARD (Admin)
        // ============================================================
        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction("Admin", "Home");
        }

        [HttpGet]
        public IActionResult Dashboard()
        {
            return RedirectToAction("Admin", "Home");
        }

        // ============================================================
        // COURT MANAGEMENT — หน้าจัดการสนามแบดมินตัน (เฉพาะ Admin)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Court()
        {
            ViewData["Title"] = "จัดการสนามแบดมินตัน";
            var courts = await _db.Courts
                .OrderBy(c => c.CourtId)
                .ToListAsync();

            if (!courts.Any())
            {
                var defaultCourts = new List<Court>
                {
                    new() { CourtName = "Court 1", Description = "พื้นยาง BWF มาตรฐานสากล", PricePerHour = 200, Status = "AVAILABLE", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                    new() { CourtName = "Court 2", Description = "พื้นยาง BWF มาตรฐานสากล", PricePerHour = 200, Status = "AVAILABLE", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                    new() { CourtName = "Court 3", Description = "พื้นยาง BWF มาตรฐานสากล", PricePerHour = 200, Status = "AVAILABLE", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now },
                    new() { CourtName = "Court 4", Description = "พื้นยาง BWF มาตรฐานสากล", PricePerHour = 200, Status = "AVAILABLE", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now }
                };
                _db.Courts.AddRange(defaultCourts);
                await _db.SaveChangesAsync();
                courts = await _db.Courts.OrderBy(c => c.CourtId).ToListAsync();
            }

            return View(courts);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCourtStatus(ushort courtId)
        {
            var court = await _db.Courts.FindAsync(courtId);
            if (court != null)
            {
                court.Status = court.Status == "AVAILABLE" ? "MAINTENANCE" : "AVAILABLE";
                court.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = $"เปลี่ยนสถานะ {court.CourtName} เป็น {(court.Status == "AVAILABLE" ? "เปิดให้บริการ" : "ปิดปรับปรุง")} สำเร็จ";
            }
            return RedirectToAction(nameof(Court));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCourtPrice(ushort courtId, decimal pricePerHour, string? description)
        {
            var court = await _db.Courts.FindAsync(courtId);
            if (court != null)
            {
                court.PricePerHour = pricePerHour;
                if (!string.IsNullOrWhiteSpace(description))
                {
                    court.Description = description.Trim();
                }
                court.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = $"อัปเดตราคา {court.CourtName} เป็น {pricePerHour:N0} บาท/ชม. สำเร็จ";
            }
            return RedirectToAction(nameof(Court));
        }

        // ============================================================
        // PRODUCT MANAGEMENT — หน้าจัดการสินค้าและอุปกรณ์ (เฉพาะ Admin)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Product()
        {
            ViewData["Title"] = "จัดการสินค้าและอุปกรณ์";
            var products = await _db.Products
                .Include(p => p.Category)
                .OrderBy(p => p.CategoryId)
                .ThenBy(p => p.ProductId)
                .ToListAsync();

            ViewBag.Categories = await _db.ProductCategories.ToListAsync();
            return View(products);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProductPrice(uint productId, decimal price, decimal deposit)
        {
            var product = await _db.Products.FindAsync(productId);
            if (product != null)
            {
                product.Price = price;
                product.Deposit = deposit;
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = $"อัปเดตราคา {product.ProductName} สำเร็จ";
            }
            return RedirectToAction(nameof(Product));
        }

        // ============================================================
        // PROMOTION MANAGEMENT — หน้าจัดการโปรโมชั่น 5 แบบ (เฉพาะ Admin)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Promotion()
        {
            ViewData["Title"] = "จัดการโปรโมชั่น";

            // ตรวจสอบสิทธิ์ Admin (ถ้ามีระบบ Login)
            bool isLoggedIn = TempData.Peek("IsLoggedIn") is bool b && b;
            ViewBag.IsAdmin = true; // อนุญาตให้ Admin จัดการโปรโมชั่น

            // ตรวจสอบและเตรียมข้อมูลโปรโมชั่น 5 แบบมาตรฐานหากยังไม่มีในระบบ
            await EnsureStandardPromotionsAsync();

            var promotions = await _db.Promotions
                .Include(p => p.PromotionRules)
                .ThenInclude(r => r.Category)
                .OrderBy(p => p.Priority)
                .ToListAsync();

            return View(promotions);
        }

        // ============================================================
        // UPDATE PROMOTION — อัปเดตตัวเลขส่วนลดและเงื่อนไข (เปลี่ยนทั้งเว็บ)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePromotion(
            uint promotionId,
            string promoName,
            string promoCode,
            string? description,
            decimal discountValue,
            string discountType,
            decimal? minBillAmount,
            string? timeStart,
            string? timeEnd,
            bool isActive,
            uint? requiredQty,
            uint? nthItem)
        {
            var promo = await _db.Promotions
                .Include(p => p.PromotionRules)
                .FirstOrDefaultAsync(p => p.PromotionId == promotionId);

            if (promo == null)
            {
                TempData["ErrorMessage"] = "ไม่พบโปรโมชั่นที่ต้องการแก้ไข";
                return RedirectToAction(nameof(Promotion));
            }

            // อัปเดตข้อมูลโปรโมชั่นหลัก
            promo.PromoName = promoName.Trim();
            promo.PromoCode = promoCode.Trim().ToUpper();
            promo.Description = description?.Trim();
            promo.DiscountValue = discountValue;
            promo.DiscountType = discountType;
            promo.MinBillAmount = minBillAmount;
            promo.IsActive = isActive;
            promo.UpdatedAt = DateTime.Now;

            // อัปเดตช่วงเวลา (สำหรับโปรฯ Time-based)
            if (!string.IsNullOrWhiteSpace(timeStart) && TimeOnly.TryParse(timeStart, out var tStart))
            {
                promo.TimeStart = tStart;
            }
            if (!string.IsNullOrWhiteSpace(timeEnd) && TimeOnly.TryParse(timeEnd, out var tEnd))
            {
                promo.TimeEnd = tEnd;
            }

            // อัปเดตกฎเงื่อนไข (PromotionRules)
            if (requiredQty.HasValue || nthItem.HasValue)
            {
                foreach (var rule in promo.PromotionRules)
                {
                    if (requiredQty.HasValue && rule.RequiredQty != requiredQty.Value)
                    {
                        rule.RequiredQty = requiredQty.Value;
                    }
                    if (nthItem.HasValue)
                    {
                        rule.NthItem = nthItem.Value;
                    }
                }
            }

            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = $"✓ บันทึกการแก้ไขโปรโมชั่น \"{promo.PromoName}\" สำเร็จ! การเปลี่ยนแปลงมีผลทั่วทั้งระบบแล้ว";
            return RedirectToAction(nameof(Promotion));
        }

        // ============================================================
        // TOGGLE ACTIVE — เปิด/ปิด การใช้งานโปรโมชั่นแบบทันที
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePromoStatus(uint id)
        {
            var promo = await _db.Promotions.FindAsync(id);
            if (promo != null)
            {
                promo.IsActive = !(promo.IsActive ?? false);
                promo.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync();
                string statusText = promo.IsActive == true ? "เปิดใช้งาน" : "ปิดใช้งาน";
                TempData["SuccessMessage"] = $"เปลี่ยนสถานะโปรโมชั่น \"{promo.PromoName}\" เป็น {statusText} เรียบร้อยแล้ว";
            }
            return RedirectToAction(nameof(Promotion));
        }

        // ============================================================
        // SEED/ENSURE: สร้างโปรโมชั่น 5 แบบมาตรฐานตามโจทย์หากยังไม่มี
        // ============================================================
        private async Task EnsureStandardPromotionsAsync()
        {
            // ตรวจสอบหมวดหมู่สินค้าที่เกี่ยวข้อง
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

            await _db.SaveChangesAsync();

            // 1. โปรฯ เช่าสนาม (Time-based): 09.00 - 15.00 น. ลด 20%
            if (!await _db.Promotions.AnyAsync(p => p.PromoType == "TIME_BASED"))
            {
                var p1 = new Promotion
                {
                    PromoCode = "COURT_OFFPEAK",
                    PromoName = "โปรฯ เช่าสนาม (Time-based)",
                    Description = "จองคอร์ทช่วงเวลา 09.00 - 15.00 น. (จันทร์-ศุกร์) ลดราคาค่าเช่าคอร์ท 20%",
                    PromoType = "TIME_BASED",
                    DiscountType = "PERCENT",
                    DiscountValue = 20,
                    TimeStart = new TimeOnly(9, 0),
                    TimeEnd = new TimeOnly(15, 0),
                    Priority = 1,
                    IsActive = true,
                    IsStackable = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                p1.PromotionRules.Add(new PromotionRule
                {
                    RuleKind = "REWARD",
                    TargetType = "COURT_FEE",
                    RequiredQty = 1
                });
                _db.Promotions.Add(p1);
            }

            // 2. โปรฯ ส่งเสริมการขาย (Volume-based Sale): ซื้อลูกแบด 2 หลอดขึ้นไป หลอดที่ 2 ลด 50%
            if (!await _db.Promotions.AnyAsync(p => p.PromoType == "VOLUME_SALE"))
            {
                var p2 = new Promotion
                {
                    PromoCode = "SHUTTLE_2ND_50",
                    PromoName = "โปรฯ ส่งเสริมการขาย (Volume-based Sale)",
                    Description = "หากลูกค้าซื้อลูกแบดมินตันรุ่นใดก็ได้ตั้งแต่ 2 หลอดขึ้นไป ระบบจะลดราคาลูกแบดหลอดที่ 2 ทันที 50%",
                    PromoType = "VOLUME_SALE",
                    DiscountType = "PERCENT",
                    DiscountValue = 50,
                    Priority = 2,
                    IsActive = true,
                    IsStackable = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                p2.PromotionRules.Add(new PromotionRule
                {
                    RuleKind = "CONDITION",
                    TargetType = "SALE_ITEM",
                    CategoryId = catShuttle.CategoryId,
                    RequiredQty = 2,
                    NthItem = 2
                });
                p2.PromotionRules.Add(new PromotionRule
                {
                    RuleKind = "REWARD",
                    TargetType = "SALE_ITEM",
                    CategoryId = catShuttle.CategoryId,
                    RequiredQty = 1,
                    NthItem = 2
                });
                _db.Promotions.Add(p2);
            }

            // 3. โปรฯ ส่งเสริมการเช่า (Bundle Rental): เช่าไม้ 2 ไม้ + เช่ารองเท้า 2 คู่ ลด 20%
            if (!await _db.Promotions.AnyAsync(p => p.PromoType == "BUNDLE_RENTAL"))
            {
                var p3 = new Promotion
                {
                    PromoCode = "BUNDLE_RENT_20",
                    PromoName = "โปรฯ ส่งเสริมการเช่า (Bundle Rental)",
                    Description = "หากลูกค้าเช่าไม้แบดมินตัน 2 ไม้ พร้อมกับเช่ารองเท้า 2 คู่ ในบิลเดียวกัน ระบบจะลดราคาค่าเช่าอุปกรณ์เซ็ตนี้ 20%",
                    PromoType = "BUNDLE_RENTAL",
                    DiscountType = "PERCENT",
                    DiscountValue = 20,
                    Priority = 3,
                    IsActive = true,
                    IsStackable = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                p3.PromotionRules.Add(new PromotionRule
                {
                    RuleKind = "CONDITION",
                    TargetType = "RENT_ITEM",
                    CategoryId = catRacket.CategoryId,
                    RequiredQty = 2
                });
                p3.PromotionRules.Add(new PromotionRule
                {
                    RuleKind = "CONDITION",
                    TargetType = "RENT_ITEM",
                    CategoryId = catShoes.CategoryId,
                    RequiredQty = 2
                });
                p3.PromotionRules.Add(new PromotionRule
                {
                    RuleKind = "REWARD",
                    TargetType = "RENT_ITEM",
                    RequiredQty = 4
                });
                _db.Promotions.Add(p3);
            }

            // 4. โปรฯ ซื้อคู่เช่า (Cross-selling Buy & Rent): ซื้อลูกแบด 1 หลอด + เช่าไม้ 2 ไม้ รับสิทธิ์เช่ารองเท้าฟรี 1 คู่
            if (!await _db.Promotions.AnyAsync(p => p.PromoType == "CROSS_SELL"))
            {
                var p4 = new Promotion
                {
                    PromoCode = "CROSS_FREE_SHOES",
                    PromoName = "โปรฯ ซื้อคู่เช่า (Cross-selling Buy & Rent)",
                    Description = "หากลูกค้าซื้อลูกแบดมินตัน 1 หลอด + เช่าไม้แบดมินตัน 2 ไม้ รับสิทธิ์เช่ารองเท้าฟรี 1 คู่ (ระบบตัดราคารองเท้าออก 1 คู่)",
                    PromoType = "CROSS_SELL",
                    DiscountType = "FREE_ITEM",
                    DiscountValue = 1,
                    Priority = 4,
                    IsActive = true,
                    IsStackable = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                p4.PromotionRules.Add(new PromotionRule
                {
                    RuleKind = "CONDITION",
                    TargetType = "SALE_ITEM",
                    CategoryId = catShuttle.CategoryId,
                    RequiredQty = 1
                });
                p4.PromotionRules.Add(new PromotionRule
                {
                    RuleKind = "CONDITION",
                    TargetType = "RENT_ITEM",
                    CategoryId = catRacket.CategoryId,
                    RequiredQty = 2
                });
                p4.PromotionRules.Add(new PromotionRule
                {
                    RuleKind = "REWARD",
                    TargetType = "RENT_ITEM",
                    CategoryId = catShoes.CategoryId,
                    RequiredQty = 1
                });
                _db.Promotions.Add(p4);
            }

            // 5. โปรฯ ยอดซื้อรวม (Total Bill Discount): สมาชิกใหม่จองครั้งแรก บิล 600+ ลด 100 บาท
            if (!await _db.Promotions.AnyAsync(p => p.PromoType == "TOTAL_BILL"))
            {
                var p5 = new Promotion
                {
                    PromoCode = "NEW_MEMBER_100",
                    PromoName = "โปรฯ ยอดซื้อรวม (Total Bill Discount)",
                    Description = "สมาชิกใหม่ที่จองคอร์ทครั้งแรก และมียอดรวมในบิล (ทั้งค่าเช่าคอร์ท + ค่าซื้อของ + ค่าเช่าของ) ตั้งแต่ 600 บาทขึ้นไป รับส่วนลดหักท้ายบิลทันที 100 บาท",
                    PromoType = "TOTAL_BILL",
                    DiscountType = "FIXED_AMOUNT",
                    DiscountValue = 100,
                    MinBillAmount = 600,
                    FirstBookingOnly = true,
                    Priority = 5,
                    IsActive = true,
                    IsStackable = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                p5.PromotionRules.Add(new PromotionRule
                {
                    RuleKind = "REWARD",
                    TargetType = "BILL_TOTAL",
                    RequiredQty = 1
                });
                _db.Promotions.Add(p5);
            }

            await _db.SaveChangesAsync();
        }
    }
}

