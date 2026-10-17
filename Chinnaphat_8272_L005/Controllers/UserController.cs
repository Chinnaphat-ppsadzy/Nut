using Microsoft.AspNetCore.Mvc;
using Chinnaphat_8272_L005.Models;
using Chinnaphat_8272_L005.ViewModels;

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

                // UPDATE — บันทึกเวลาล็อกอินล่าสุดลงฐานข้อมูล
                user.LastLoginAt = DateTime.Now;
                _db.SaveChanges();

                // เก็บข้อมูลผู้ใช้ที่ล็อกอินผ่าน TempData
                TempData["IsLoggedIn"]  = true;
                TempData["UserId"]      = user.UserId.ToString();
                TempData["Username"]    = user.Email;
                TempData["FullName"]    = user.FullName;
                TempData["Email"]       = user.Email;
                TempData["LoginTime"]   = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                TempData["RememberMe"]  = model.RememberMe;

                TempData["SuccessMessage"] =
                    $"เข้าสู่ระบบสำเร็จ! ยินดีต้อนรับ {user.FullName}";

                return RedirectToAction("Index", "Home");
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
                                   Role      = rg != null ? rg.RoleName : "User",
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

            ViewBag.Roles  = (from r in _db.Roles select r).ToList();
            ViewBag.EditId = id;

            var model = new AddUserViewModel
            {
                FirstName = user.FullName.Contains(" ")
                    ? user.FullName.Split(' ')[0] : user.FullName,
                LastName  = user.FullName.Contains(" ")
                    ? string.Join(" ", user.FullName.Split(' ').Skip(1)) : "",
                Email     = user.Email,
                Username  = user.Email,
                Phone     = user.Phone ?? "",
                IsActive  = user.Status == "active"
            };

            return View("EditUser", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditUser(int id, AddUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                // READ — ค้นหา User ที่ต้องการแก้ไข
                var user = (from u in _db.Users
                            where u.UserId == (uint)id
                            select u).FirstOrDefault();

                if (user != null)
                {
                    // READ — ค้นหา Role จาก RoleCode
                    var role = (from r in _db.Roles
                                where r.RoleCode == model.Role
                                select r).FirstOrDefault();

                    // UPDATE — อัปเดตข้อมูลในฐานข้อมูล
                    user.FullName  = $"{model.FirstName} {model.LastName}";
                    user.Email     = model.Email;
                    user.Phone     = model.Phone;
                    user.Status    = model.IsActive ? "active" : "inactive";
                    user.UpdatedAt = DateTime.Now;
                    if (role != null) user.RoleId = role.RoleId;
                    if (!string.IsNullOrEmpty(model.Password))
                        user.PasswordHash = model.Password;

                    _db.SaveChanges();
                    TempData["SuccessMessage"] = $"แก้ไขข้อมูลผู้ใช้ {user.Email} สำเร็จ!";
                }

                return RedirectToAction("UserList");
            }

            ViewBag.Roles  = (from r in _db.Roles select r).ToList();
            ViewBag.EditId = id;
            ViewData["Title"] = "แก้ไขผู้ใช้งาน";
            return View("EditUser", model);
        }
    }
}