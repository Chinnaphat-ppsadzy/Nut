using System.ComponentModel.DataAnnotations;

namespace Chinnaphat_8272_L005.ViewModels
{
    // ============================================================
    // ViewModel สำหรับแสดงผู้ใช้แต่ละคนในหน้า UserList
    // ============================================================
    public class UserListItemViewModel
    {
        public int Id { get; set; }

        [Display(Name = "ชื่อ-นามสกุล")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "ชื่อผู้ใช้")]
        public string Username { get; set; } = string.Empty;

        [Display(Name = "อีเมล")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "บทบาท")]
        public string Role { get; set; } = "User";

        [Display(Name = "เบอร์โทร")]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "เพศ")]
        public string Gender { get; set; } = string.Empty;

        [Display(Name = "สถานะ")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "วันที่สมัคร")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // แสดงว่าผู้ใช้นี้มาจาก Register หรือ AddUser
        public string Source { get; set; } = "Register";
    }

    // ============================================================
    // ViewModel หลักสำหรับหน้า UserList (รวม list + ผู้ใช้ที่ล็อกอินอยู่)
    // ============================================================
    public class UserListViewModel
    {
        // รายชื่อผู้ใช้ทั้งหมด
        public List<UserListItemViewModel> Users { get; set; } = new();

        // ข้อมูลผู้ใช้ที่ล็อกอินอยู่ (จาก Login ViewModel)
        public string LoggedInUsername { get; set; } = string.Empty;
        public bool IsLoggedIn { get; set; } = false;

        // สรุปจำนวน
        public int TotalUsers => Users.Count;
        public int ActiveUsers => Users.Count(u => u.IsActive);
        public int AdminCount => Users.Count(u => u.Role == "Admin");
    }
}
