using Microsoft.AspNetCore.Mvc;
using Chinnaphat_8272_L005.Models;
using Chinnaphat_8272_L005.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Net;

namespace Chinnaphat_8272_L005.Controllers
{
    public class UserController : Controller
    {
        private readonly Csi402dbContext _db;

        public UserController(Csi402dbContext db)
        {
            _db = db;
        }

        // ============================================================
        // LOGIN — READ: ตรวจสอบ Email + Password จากฐานข้อมูล
        // ============================================================

        [HttpGet]
        public IActionResult Login()
        {
            ViewData["Title"] = "เข้าสู่ระบบ";
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // READ — ค้นหา User จากฐานข้อมูลด้วย Email และ Password
                var user = (from u in _db.Users
                            where u.Email == model.Email
                               && u.PasswordHash == model.Password
                            select u).FirstOrDefault();

                if (user == null)
                {
                    ViewBag.ErrorMessage = "อีเมลหรือรหัสผ่านไม่ถูกต้อง กรุณาลองใหม่อีกครั้ง";
                    ViewData["Title"] = "เข้าสู่ระบบ";
                    return View(model);
                }

                // ตรวจสอบสถานะ: ห้ามเข้าถ้า status เป็น inactive/banned/disabled เท่านั้น
                // อนุญาตทุก status อื่น (active, Active, ACTIVE, null, ว่าง, ฯลฯ)
                var statusLower = (user.Status ?? "").ToLower().Trim();
                var blockedStatuses = new[] { "inactive", "banned", "disabled", "suspended" };
                if (blockedStatuses.Contains(statusLower))
                {
                    ViewBag.ErrorMessage = $"บัญชีนี้ถูกระงับการใช้งาน (สถานะ: {user.Status}) กรุณาติดต่อผู้ดูแลระบบ";
                    ViewData["Title"] = "เข้าสู่ระบบ";
                    return View(model);
                }

                // READ — ดึง Role ของผู้ใช้เพื่อพาไปยังหน้าระบบที่ถูกต้อง
                var userRole = (from r in _db.Roles
                                where r.RoleId == user.RoleId
                                select r).FirstOrDefault();
                string roleCode = userRole?.RoleCode ?? "CUSTOMER";

                // UPDATE — บันทึกเวลาล็อกอินล่าสุดลงฐานข้อมูล
                user.LastLoginAt = DateTime.Now;
                _db.SaveChanges();

                // เก็บข้อมูลผู้ใช้ที่ล็อกอินผ่าน Session และ Cookies
                try
                {
                    HttpContext.Session.SetString("UserId", user.UserId.ToString());
                    HttpContext.Session.SetString("FullName", user.FullName);
                    HttpContext.Session.SetString("Email", user.Email);
                    HttpContext.Session.SetString("RoleCode", roleCode);
                    HttpContext.Session.SetString("RoleName", userRole?.RoleName ?? roleCode);
                }
                catch {}

                var cookieOptions = new CookieOptions
                {
                    Expires = model.RememberMe ? DateTimeOffset.Now.AddDays(30) : DateTimeOffset.Now.AddHours(24),
                    HttpOnly = false,
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax
                };
                Response.Cookies.Append("cb_user_id", user.UserId.ToString(), cookieOptions);
                Response.Cookies.Append("cb_user_name", WebUtility.UrlEncode(user.FullName), cookieOptions);
                Response.Cookies.Append("cb_email", user.Email, cookieOptions);
                Response.Cookies.Append("cb_role_code", roleCode, cookieOptions);
                Response.Cookies.Append("cb_role_name", WebUtility.UrlEncode(userRole?.RoleName ?? roleCode), cookieOptions);

                // เก็บข้อมูลผู้ใช้ที่ล็อกอินผ่าน TempData
                TempData["IsLoggedIn"]  = true;
                TempData["UserId"]      = user.UserId.ToString();
                TempData["Username"]    = user.Email;
                TempData["FullName"]    = user.FullName;
                TempData["Email"]       = user.Email;
                TempData["RoleCode"]    = roleCode;
                TempData["RoleName"]    = userRole?.RoleName ?? roleCode;
                TempData["LoginTime"]   = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                TempData["RememberMe"]  = model.RememberMe;

                TempData["SuccessMessage"] =
                    $"เข้าสู่ระบบสำเร็จ! ยินดีต้อนรับ {user.FullName} ({userRole?.RoleName ?? roleCode})";

                // นำผู้ใช้ไปยังหน้าระบบตามบทบาทของตนเอง
                if (roleCode == "SALES_STAFF")
                {
                    return RedirectToAction("Sales", "SalesStaff");
                }
                else if (roleCode == "ADMIN")
                {
                    return RedirectToAction("Admin", "Home");
                }
                else
                {
                    return RedirectToAction("Customer", "Home");
                }
            }

            ViewData["Title"] = "เข้าสู่ระบบ";
            return View(model);
        }


        // ============================================================
        // REGISTER — CREATE: บันทึกผู้ใช้ใหม่ลงฐานข้อมูล
        // ============================================================

        [HttpGet]
        public IActionResult Register()
        {
            ViewData["Title"] = "สมัครสมาชิก";
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // READ — ตรวจสอบว่า Email ซ้ำหรือไม่
                var existingUser = (from u in _db.Users
                                    where u.Email == model.Email
                                    select u).FirstOrDefault();

                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "อีเมลนี้ถูกใช้งานแล้ว");
                    ViewData["Title"] = "สมัครสมาชิก";
                    return View(model);
                }

                // READ — ดึง RoleId ของ "CUSTOMER"
                var customerRole = (from r in _db.Roles
                                    where r.RoleCode == "CUSTOMER"
                                    select r).FirstOrDefault();

                byte roleId = customerRole != null ? customerRole.RoleId : (byte)1;

                // CREATE — สร้าง User ใหม่แล้วบันทึกลงฐานข้อมูล
                var newUser = new User
                {
                    RoleId       = roleId,
                    Email        = model.Email,
                    PasswordHash = model.Password,
                    FullName     = $"{model.FirstName} {model.LastName}",
                    Phone        = model.Phone,
                    Status       = "active",
                    HasBooked    = false,
                    CreatedAt    = DateTime.Now,
                    UpdatedAt    = DateTime.Now
                };

                _db.Users.Add(newUser);
                _db.SaveChanges();

                TempData["SuccessMessage"] =
                    $"สมัครสมาชิกสำเร็จ! ยินดีต้อนรับ {model.FirstName} กรุณาเข้าสู่ระบบ";

                return RedirectToAction("Login");
            }

            ViewData["Title"] = "สมัครสมาชิก";
            return View(model);
        }


        // ============================================================
        // ADD USER (Admin) — CREATE: เพิ่มผู้ใช้ใหม่ลงฐานข้อมูล
        // ============================================================

        [HttpGet]
        public IActionResult AddUser()
        {
            ViewData["Title"] = "เพิ่มผู้ใช้งาน";
            // READ — ดึงรายการ Role จากฐานข้อมูล
            ViewBag.Roles = (from r in _db.Roles select r).ToList();
            return View(new AddUserViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddUser(AddUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                // READ — ตรวจสอบว่า Email ซ้ำหรือไม่
                var existingUser = (from u in _db.Users
                                    where u.Email == model.Email
                                    select u).FirstOrDefault();

                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "อีเมลนี้ถูกใช้งานแล้ว");
                    ViewBag.Roles = (from r in _db.Roles select r).ToList();
                    ViewData["Title"] = "เพิ่มผู้ใช้งาน";
                    return View(model);
                }

                // READ — ค้นหา RoleId จาก RoleCode
                var role = (from r in _db.Roles
                            where r.RoleCode == model.Role
                            select r).FirstOrDefault();

                byte roleId = role != null ? role.RoleId : (byte)1;

                // CREATE — บันทึกผู้ใช้ใหม่ลงฐานข้อมูล
                var newUser = new User
                {
                    RoleId       = roleId,
                    Email        = model.Email,
                    PasswordHash = model.Password,
                    FullName     = $"{model.FirstName} {model.LastName}",
                    Phone        = model.Phone,
                    Status       = model.IsActive ? "active" : "inactive",
                    HasBooked    = false,
                    CreatedAt    = DateTime.Now,
                    UpdatedAt    = DateTime.Now
                };

                _db.Users.Add(newUser);
                _db.SaveChanges();

                TempData["SuccessMessage"] = $"เพิ่มผู้ใช้ {model.Email} สำเร็จ!";
                return RedirectToAction("UserList");
            }

            ViewBag.Roles = (from r in _db.Roles select r).ToList();
            ViewData["Title"] = "เพิ่มผู้ใช้งาน";
            return View(model);
        }


        // ============================================================
        // USER LIST — READ: ดึงรายชื่อผู้ใช้ทั้งหมดจากฐานข้อมูล
        // ============================================================

        public IActionResult UserList()
        {
            ViewData["Title"] = "รายชื่อผู้ใช้งาน";

            // READ — ดึงข้อมูลผู้ใช้ทั้งหมดจากฐานข้อมูลพร้อม Role
            var usersFromDb = (from u in _db.Users
                               join r in _db.Roles on u.RoleId equals r.RoleId into roleGroup
                               from rg in roleGroup.DefaultIfEmpty()
                               orderby u.CreatedAt descending
                               select new UserListItemViewModel
                               {
                                   Id        = (int)u.UserId,
                                   FullName  = u.FullName,
                                   Username  = u.Email,
                                   Email     = u.Email,
                                   Role      = rg != null ? rg.RoleName : "ผู้ใช้ทั่วไป",
                                   RoleCode  = rg != null ? rg.RoleCode : "CUSTOMER",
                                   Phone     = u.Phone ?? "-",
                                   Gender    = "-",
                                   IsActive  = u.Status == "active",
                                   CreatedAt = u.CreatedAt,
                                   Source    = "Database"
                               }).ToList();

            var viewModel = new UserListViewModel
            {
                Users            = usersFromDb,
                IsLoggedIn       = TempData.Peek("IsLoggedIn") is bool b && b,
                LoggedInUsername = TempData.Peek("FullName") as string ?? string.Empty
            };

            return View(viewModel);
        }


        // ============================================================
        // DELETE USER — DELETE: ลบผู้ใช้ออกจากฐานข้อมูล
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteUser(int id)
        {
            // READ — ค้นหา User ที่ต้องการลบ
            var user = (from u in _db.Users
                        where u.UserId == (uint)id
                        select u).FirstOrDefault();

            if (user != null)
            {
                // DELETE — ลบออกจากฐานข้อมูล
                _db.Users.Remove(user);
                _db.SaveChanges();
                TempData["SuccessMessage"] = $"ลบผู้ใช้ {user.Email} สำเร็จ!";
            }
            else
            {
                TempData["ErrorMessage"] = "ไม่พบผู้ใช้ที่ต้องการลบ";
            }

            return RedirectToAction("UserList");
        }


        // ============================================================
        // EDIT USER — READ+UPDATE: แก้ไขข้อมูลผู้ใช้ในฐานข้อมูล
        // ============================================================

        [HttpGet]
        public IActionResult EditUser(int id)
        {
            ViewData["Title"] = "แก้ไขผู้ใช้งาน";

            // READ — ดึงข้อมูล User จากฐานข้อมูล
            var user = (from u in _db.Users
                        where u.UserId == (uint)id
                        select u).FirstOrDefault();

            if (user == null)
                return RedirectToAction("UserList");

            ViewBag.Roles = (from r in _db.Roles select r).ToList();

            var currentRole = (from r in _db.Roles
                               where r.RoleId == user.RoleId
                               select r).FirstOrDefault();

            // READ — แปลง User entity ไปเป็น EditUserViewModel
            var model = new EditUserViewModel
            {
                UserId    = id,
                FirstName = user.FullName.Contains(" ")
                    ? user.FullName.Split(' ')[0] : user.FullName,
                LastName  = user.FullName.Contains(" ")
                    ? string.Join(" ", user.FullName.Split(' ').Skip(1)) : "",
                Email     = user.Email,
                Phone     = user.Phone ?? "",
                Role      = currentRole?.RoleCode ?? "CUSTOMER",
                IsActive  = user.Status == "active"
            };

            return View("EditUser", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditUser(int id, EditUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                // READ — ค้นหา User ที่ต้องการแก้ไข
                var user = (from u in _db.Users
                            where u.UserId == (uint)id
                            select u).FirstOrDefault();

                if (user != null)
                {
                    // READ — ค้นหา Role จาก RoleCode (ถ้ามีการเลือกมา)
                    if (!string.IsNullOrEmpty(model.Role))
                    {
                        var role = (from r in _db.Roles
                                    where r.RoleCode == model.Role
                                    select r).FirstOrDefault();
                        if (role != null) user.RoleId = role.RoleId;
                    }

                    // UPDATE — อัปเดตข้อมูลในฐานข้อมูล
                    user.FullName  = $"{model.FirstName} {model.LastName}";
                    user.Email     = model.Email;
                    user.Phone     = model.Phone;
                    user.Status    = model.IsActive ? "active" : "inactive";
                    user.UpdatedAt = DateTime.Now;

                    // UPDATE Password เฉพาะเมื่อกรอกมาเท่านั้น
                    if (!string.IsNullOrEmpty(model.Password))
                        user.PasswordHash = model.Password;

                    _db.SaveChanges();
                    TempData["SuccessMessage"] = $"\u2713 แก้ไขข้อมูลผู้ใช้ {user.Email} สำเร็จ!";
                    return RedirectToAction("UserList");
                }

                TempData["ErrorMessage"] = "ไม่พบผู้ใช้ที่ต้องการแก้ไข";
                return RedirectToAction("UserList");
            }

            // ModelState ไม่ผ่าน — โหลด Roles ใหม่แล้ว return view เดิม
            ViewBag.Roles = (from r in _db.Roles select r).ToList();
            model.UserId  = id;
            ViewData["Title"] = "แก้ไขผู้ใช้งาน";
            return View("EditUser", model);
        }

        // ============================================================
        // LOGOUT — ออกจากระบบ ล้าง Session และ Cookies
        // ============================================================
        [HttpGet]
        public IActionResult Logout()
        {
            try
            {
                HttpContext.Session.Clear();
            }
            catch {}

            // ล้างข้อมูล TempData ทั้งหมด
            TempData.Remove("IsLoggedIn");
            TempData.Remove("UserId");
            TempData.Remove("Username");
            TempData.Remove("FullName");
            TempData.Remove("Email");
            TempData.Remove("RoleCode");
            TempData.Remove("RoleName");
            TempData.Clear();

            // ล้างคุกกี้ทั้งหมดพร้อมระบุ Path = "/"
            var deleteOptions = new CookieOptions
            {
                Expires = DateTimeOffset.Now.AddDays(-1),
                Path = "/"
            };

            Response.Cookies.Delete("cb_user_id", deleteOptions);
            Response.Cookies.Delete("cb_user_name", deleteOptions);
            Response.Cookies.Delete("cb_email", deleteOptions);
            Response.Cookies.Delete("cb_role_code", deleteOptions);
            Response.Cookies.Delete("cb_role_name", deleteOptions);
            Response.Cookies.Delete("cb_phone", deleteOptions);

            Response.Cookies.Delete("cb_user_id");
            Response.Cookies.Delete("cb_user_name");
            Response.Cookies.Delete("cb_email");
            Response.Cookies.Delete("cb_role_code");
            Response.Cookies.Delete("cb_role_name");
            Response.Cookies.Delete("cb_phone");

            TempData["SuccessMessage"] = "ออกจากระบบเรียบร้อยแล้ว เข้าสู่โหมดผู้เยี่ยมชม (Guest)";
            return RedirectToAction("Customer", "Home");
        }

        // ============================================================
        // PROFILE — แสดงข้อมูลสมาชิก ประวัติการจอง และประวัติซื้อ/เช่า
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            ViewData["Title"] = "โปรไฟล์ของฉัน & ประวัติการใช้งาน";

            // 1. ตรวจสอบ User ID จาก Session หรือ Cookies
            string? uidStr = HttpContext.Session.GetString("UserId") ?? Request.Cookies["cb_user_id"];
            uint targetUid = 0;
            if (!string.IsNullOrEmpty(uidStr))
            {
                uint.TryParse(uidStr, out targetUid);
            }

            User? user = null;
            if (targetUid > 0)
            {
                user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == targetUid);
            }

            if (user == null)
            {
                var cookieEmail = Request.Cookies["cb_email"];
                if (!string.IsNullOrEmpty(cookieEmail))
                {
                    user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == cookieEmail);
                }
            }

            if (user == null)
            {
                TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบเพื่อดูหน้าโปรไฟล์และประวัติการจอง";
                return RedirectToAction("Login");
            }

            var model = new ProfileViewModel
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone ?? "-",
                RoleName = user.Role?.RoleName ?? "ลูกค้าทั่วไป",
                RoleCode = user.Role?.RoleCode ?? "CUSTOMER",
                Status = user.Status ?? "active",
                CreatedAt = user.CreatedAt
            };

            // 2. ดึงประวัติการจองของผู้ใช้นี้
            var bookings = await _db.Bookings
                .Where(b => b.UserId == user.UserId)
                .Include(b => b.BookingSlots).ThenInclude(bs => bs.Court)
                .Include(b => b.BookingSlots).ThenInclude(bs => bs.Slot)
                .Include(b => b.BookingItems).ThenInclude(bi => bi.Product).ThenInclude(p => p.Category)
                .Include(b => b.Payments)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            model.TotalBookingsCount = bookings.Count;
            model.TotalSpending = bookings.Sum(b => b.NetAmount);

            foreach (var b in bookings)
            {
                var cNames = string.Join(", ", b.BookingSlots.Select(s => s.Court?.CourtName ?? $"Court {s.CourtId}").Distinct());
                var tSlots = string.Join(", ", b.BookingSlots.Select(s => s.Slot != null ? $"{s.Slot.StartTime:HH:mm}-{s.Slot.EndTime:HH:mm}" : $"Slot {s.SlotId}"));

                string sText = "รอชำระเงิน";
                string sBadge = "bg-warning text-dark";
                switch (b.Status?.ToUpper())
                {
                    case "PENDING_VERIFICATION":
                        sText = "รอฝ่ายบัญชีตรวจสอบสลิป";
                        sBadge = "bg-warning text-dark";
                        break;
                    case "CONFIRMED":
                        sText = "ยืนยันคิวแล้ว (พร้อมใช้สนาม)";
                        sBadge = "bg-success text-white";
                        break;
                    case "CHECKED_IN":
                        sText = "เช็คอินเข้าสนามแล้ว";
                        sBadge = "bg-primary text-white";
                        break;
                    case "COMPLETED":
                        sText = "เสร็จสิ้นการใช้บริการ";
                        sBadge = "bg-secondary text-white";
                        break;
                    case "CANCELLED":
                        sText = "ยกเลิกการจอง";
                        sBadge = "bg-danger text-white";
                        break;
                }

                model.CourtBookings.Add(new ProfileBookingItem
                {
                    BookingId = b.BookingId,
                    BookingCode = b.BookingCode,
                    PlayDate = b.PlayDate.ToString("dd/MM/yyyy"),
                    CourtNames = string.IsNullOrEmpty(cNames) ? "ไม่ได้จองคอร์ท" : cNames,
                    TimeSlots = tSlots,
                    CourtAmount = b.CourtAmount,
                    DiscountAmount = b.DiscountAmount,
                    NetAmount = b.NetAmount,
                    Status = b.Status,
                    StatusText = sText,
                    StatusBadgeClass = sBadge,
                    SlipImageUrl = b.Payments.OrderByDescending(p => p.PaymentId).FirstOrDefault()?.SlipImageUrl,
                    CreatedAt = b.CreatedAt
                });

                // แยกรายการสินค้าซื้อ (Purchases) และอุปกรณ์เช่า (Rentals)
                foreach (var itm in b.BookingItems)
                {
                    if (itm.ItemType == "RENT")
                    {
                        model.TotalRentalItemsCount += (int)itm.Quantity;

                        string rText = "จองไว้ (รอรับอุปกรณ์)";
                        string rBadge = "bg-warning text-dark";
                        if (itm.ItemStatus == "HANDED_OVER") { rText = "รับอุปกรณ์แล้ว"; rBadge = "bg-primary text-white"; }
                        else if (itm.ItemStatus == "RETURNED") { rText = "ส่งคืนแล้ว"; rBadge = "bg-success text-white"; }
                        else if (itm.ItemStatus == "DAMAGED") { rText = "ชำรุดเสียหาย"; rBadge = "bg-danger text-white"; }

                        model.RentalHistory.Add(new ProfileRentalItem
                        {
                            BookingItemId = itm.BookingItemId,
                            BookingCode = b.BookingCode,
                            ProductName = itm.Product?.ProductName ?? $"อุปกรณ์ #{itm.ProductId}",
                            CategoryName = itm.Product?.Category?.CategoryName ?? "อุปกรณ์เช่า",
                            Quantity = itm.Quantity,
                            UnitPrice = itm.UnitPrice,
                            Deposit = itm.Product?.Deposit ?? 0m,
                            LineTotal = itm.LineTotal,
                            ItemStatus = itm.ItemStatus,
                            ItemStatusText = rText,
                            ItemStatusBadgeClass = rBadge,
                            HandedOverAt = itm.HandedOverAt,
                            ReturnedAt = itm.ReturnedAt,
                            RentalDate = b.CreatedAt
                        });
                    }
                    else
                    {
                        model.TotalSaleItemsCount += (int)itm.Quantity;
                        model.PurchaseHistory.Add(new ProfileOrderItem
                        {
                            BookingItemId = itm.BookingItemId,
                            BookingCode = b.BookingCode,
                            ProductName = itm.Product?.ProductName ?? $"สินค้า #{itm.ProductId}",
                            CategoryName = itm.Product?.Category?.CategoryName ?? "สินค้าขาย",
                            Quantity = itm.Quantity,
                            UnitPrice = itm.UnitPrice,
                            LineTotal = itm.LineTotal,
                            ItemStatus = itm.ItemStatus,
                            OrderDate = b.CreatedAt
                        });
                    }
                }
            }

            return View(model);
        }

        // ============================================================
        // UPDATE PROFILE — แก้ไขชื่อ-นามสกุล และเบอร์โทรศัพท์
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string fullName, string phone)
        {
            string? uidStr = HttpContext.Session.GetString("UserId") ?? Request.Cookies["cb_user_id"];
            if (string.IsNullOrEmpty(uidStr) || !uint.TryParse(uidStr, out uint uid))
            {
                return RedirectToAction("Login");
            }

            var user = await _db.Users.FindAsync(uid);
            if (user != null)
            {
                if (!string.IsNullOrWhiteSpace(fullName)) user.FullName = fullName.Trim();
                if (!string.IsNullOrWhiteSpace(phone)) user.Phone = phone.Trim();
                user.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync();

                try
                {
                    HttpContext.Session.SetString("FullName", user.FullName);
                }
                catch {}
                Response.Cookies.Append("cb_user_name", WebUtility.UrlEncode(user.FullName));

                TempData["SuccessMessage"] = "อัปเดตข้อมูลส่วนตัวสำเร็จเรียบร้อย!";
            }

            return RedirectToAction("Profile");
        }
    }
}
