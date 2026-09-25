using Microsoft.AspNetCore.Mvc;
using Chinnaphat_8272_L005.Models;

namespace Chinnaphat_8272_L005.Controllers
{
    public class UserController : Controller
    {
        // ============================================================
        // LOGIN
        // ============================================================
        [HttpGet]
        public IActionResult Login()
        {
            ViewData["Title"] = "เข้าสู่ระบบ";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // TODO: ตรวจสอบกับฐานข้อมูลจริง
                TempData["SuccessMessage"] = $"เข้าสู่ระบบสำเร็จ! ยินดีต้อนรับ {model.Username}";
                return RedirectToAction("Index", "Home");
            }
            ViewData["Title"] = "เข้าสู่ระบบ";
            return View(model);
        }

        // ============================================================
        // REGISTER
        // ============================================================
        [HttpGet]
        public IActionResult Register()
        {
            ViewData["Title"] = "สมัครสมาชิก";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // TODO: บันทึกลงฐานข้อมูลจริง
                TempData["SuccessMessage"] = "สมัครสมาชิกสำเร็จ! กรุณาเข้าสู่ระบบ";
                return RedirectToAction("Login");
            }
            ViewData["Title"] = "สมัครสมาชิก";
            return View(model);
        }

        // ============================================================
        // ADD USER (Admin)
        // ============================================================
        [HttpGet]
        public IActionResult AddUser()
        {
            ViewData["Title"] = "เพิ่มผู้ใช้งาน";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddUser(AddUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                // TODO: บันทึกลงฐานข้อมูลจริง
                TempData["SuccessMessage"] = $"เพิ่มผู้ใช้ {model.Username} สำเร็จ!";
                return RedirectToAction("UserList");
            }
            ViewData["Title"] = "เพิ่มผู้ใช้งาน";
            return View(model);
        }

        // ============================================================
        // USER LIST
        // ============================================================
        public IActionResult UserList()
        {
            ViewData["Title"] = "รายชื่อผู้ใช้งาน";

            // ข้อมูลตัวอย่าง (Mock Data)
            var users = new List<UserListItem>
            {
                new UserListItem { Id = 1, FullName = "Chinnaphat Supsombat", Username = "chinnaphat", Email = "chinnaphat@email.com", Role = "Admin", Phone = "081-234-5678", IsActive = true,  CreatedAt = new DateTime(2026, 1, 10) },
                new UserListItem { Id = 2, FullName = "สมชาย ใจดี",            Username = "somchai",    Email = "somchai@email.com",    Role = "User",  Phone = "082-111-2222", IsActive = true,  CreatedAt = new DateTime(2026, 2, 15) },
                new UserListItem { Id = 3, FullName = "สมหญิง รักเรียน",       Username = "somying",    Email = "somying@email.com",    Role = "User",  Phone = "083-333-4444", IsActive = false, CreatedAt = new DateTime(2026, 3, 20) },
                new UserListItem { Id = 4, FullName = "วิชัย เก่งมาก",          Username = "wichai",     Email = "wichai@email.com",     Role = "Staff", Phone = "084-555-6666", IsActive = true,  CreatedAt = new DateTime(2026, 4, 5)  },
                new UserListItem { Id = 5, FullName = "นภา สวยงาม",            Username = "napa",       Email = "napa@email.com",       Role = "User",  Phone = "085-777-8888", IsActive = true,  CreatedAt = new DateTime(2026, 5, 1)  },
            };

            return View(users);
        }
    }
}
