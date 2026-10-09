using Microsoft.AspNetCore.Mvc;
using Chinnaphat_8272_L005.Models;
using Chinnaphat_8272_L005.ViewModels;
using System.Text.Json;

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
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // เก็บข้อมูลผู้ใช้ที่ล็อกอินผ่าน TempData
                TempData["IsLoggedIn"]  = true;
                TempData["Username"]    = model.Username;
                TempData["LoginTime"]   = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                TempData["RememberMe"]  = model.RememberMe;

                TempData["SuccessMessage"] =
                    $"เข้าสู่ระบบสำเร็จ! ยินดีต้อนรับ {model.Username}";

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
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // สร้าง UserListItemViewModel จากข้อมูลที่ Register
                var newUser = new UserListItemViewModel
                {
                    FullName  = $"{model.FirstName} {model.LastName}",
                    Username  = model.Username,
                    Email     = model.Email,
                    Phone     = model.Phone,
                    Gender    = model.Gender,
                    Role      = "User",
                    IsActive  = true,
                    CreatedAt = DateTime.Now,
                    Source    = "Register"
                };

                // โหลดรายชื่อที่มีอยู่แล้วจาก Session JSON แล้วเพิ่มเข้าไป
                var existingJson = TempData.Peek("RegisteredUsers") as string;
                var registeredUsers = string.IsNullOrEmpty(existingJson)
                    ? new List<UserListItemViewModel>()
                    : JsonSerializer.Deserialize<List<UserListItemViewModel>>(existingJson)
                      ?? new List<UserListItemViewModel>();

                // กำหนด Id ต่อจากผู้ใช้คนสุดท้าย
                newUser.Id = (registeredUsers.Count > 0
                    ? registeredUsers.Max(u => u.Id)
                    : 5) + 1;

                registeredUsers.Add(newUser);

                // บันทึกกลับลง TempData
                TempData["RegisteredUsers"] = JsonSerializer.Serialize(registeredUsers);
                TempData["NewUserName"]     = $"{model.FirstName} {model.LastName}";

                TempData["SuccessMessage"] =
                    $"สมัครสมาชิกสำเร็จ! ยินดีต้อนรับ {model.FirstName} กรุณาเข้าสู่ระบบ";

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
            return View(new AddUserViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddUser(AddUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                // สร้าง UserListItemViewModel จากข้อมูล AddUser
                var newUser = new UserListItemViewModel
                {
                    FullName  = $"{model.FirstName} {model.LastName}",
                    Username  = model.Username,
                    Email     = model.Email,
                    Phone     = model.Phone,
                    Gender    = model.Gender,
                    Role      = model.Role,
                    IsActive  = model.IsActive,
                    CreatedAt = DateTime.Now,
                    Source    = "AddUser"
                };

                // โหลดรายชื่อที่มีอยู่แล้วจาก TempData แล้วเพิ่มเข้าไป
                var existingJson = TempData.Peek("RegisteredUsers") as string;
                var registeredUsers = string.IsNullOrEmpty(existingJson)
                    ? new List<UserListItemViewModel>()
                    : JsonSerializer.Deserialize<List<UserListItemViewModel>>(existingJson)
                      ?? new List<UserListItemViewModel>();

                newUser.Id = (registeredUsers.Count > 0
                    ? registeredUsers.Max(u => u.Id)
                    : 5) + 1;

                registeredUsers.Add(newUser);
                TempData["RegisteredUsers"] = JsonSerializer.Serialize(registeredUsers);

                TempData["SuccessMessage"] =
                    $"เพิ่มผู้ใช้ {model.Username} สำเร็จ!";

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

            // ข้อมูลตัวอย่าง (Mock Data) — ผู้ใช้เริ่มต้น
            var defaultUsers = new List<UserListItemViewModel>
            {
                new() { Id = 1, FullName = "Chinnaphat Supsombat", Username = "chinnaphat",
                         Email = "chinnaphat@email.com", Role = "Admin",
                         Phone = "081-234-5678", Gender = "ชาย",
                         IsActive = true, CreatedAt = new DateTime(2026, 1, 10), Source = "Default" },

                new() { Id = 2, FullName = "สมชาย ใจดี", Username = "somchai",
                         Email = "somchai@email.com", Role = "User",
                         Phone = "082-111-2222", Gender = "ชาย",
                         IsActive = true, CreatedAt = new DateTime(2026, 2, 15), Source = "Default" },

                new() { Id = 3, FullName = "สมหญิง รักเรียน", Username = "somying",
                         Email = "somying@email.com", Role = "User",
                         Phone = "083-333-4444", Gender = "หญิง",
                         IsActive = false, CreatedAt = new DateTime(2026, 3, 20), Source = "Default" },

                new() { Id = 4, FullName = "วิชัย เก่งมาก", Username = "wichai",
                         Email = "wichai@email.com", Role = "Staff",
                         Phone = "084-555-6666", Gender = "ชาย",
                         IsActive = true, CreatedAt = new DateTime(2026, 4, 5), Source = "Default" },

                new() { Id = 5, FullName = "นภา สวยงาม", Username = "napa",
                         Email = "napa@email.com", Role = "User",
                         Phone = "085-777-8888", Gender = "หญิง",
                         IsActive = true, CreatedAt = new DateTime(2026, 5, 1), Source = "Default" }
            };

            // ดึงผู้ใช้ที่ Register / AddUser เพิ่มเข้ามา
            var existingJson = TempData.Peek("RegisteredUsers") as string;
            if (!string.IsNullOrEmpty(existingJson))
            {
                var registeredUsers = JsonSerializer.Deserialize<List<UserListItemViewModel>>(existingJson);
                if (registeredUsers != null)
                    defaultUsers.AddRange(registeredUsers);
            }

            // สร้าง UserListViewModel รวมทุกอย่าง
            var viewModel = new UserListViewModel
            {
                Users            = defaultUsers,
                IsLoggedIn       = TempData.Peek("IsLoggedIn") is bool b && b,
                LoggedInUsername = TempData.Peek("Username") as string ?? string.Empty
            };

            return View(viewModel);
        }
    }
}