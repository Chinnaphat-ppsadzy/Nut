using System.ComponentModel.DataAnnotations;

namespace Chinnaphat_8272_L005.Models
{
    // ============================================================
    // Model สำหรับ Login
    // ============================================================
    public class LoginViewModel
    {
        [Required(ErrorMessage = "กรุณากรอกชื่อผู้ใช้")]
        [Display(Name = "ชื่อผู้ใช้")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณากรอกรหัสผ่าน")]
        [DataType(DataType.Password)]
        [Display(Name = "รหัสผ่าน")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "จดจำฉัน")]
        public bool RememberMe { get; set; }
    }

    // ============================================================
    // Model สำหรับ Register
    // ============================================================
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "กรุณากรอกชื่อ")]
        [Display(Name = "ชื่อ")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณากรอกนามสกุล")]
        [Display(Name = "นามสกุล")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณากรอกอีเมล")]
        [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
        [Display(Name = "อีเมล")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณากรอกชื่อผู้ใช้")]
        [StringLength(50, MinimumLength = 4, ErrorMessage = "ชื่อผู้ใช้ต้องมี 4–50 ตัวอักษร")]
        [Display(Name = "ชื่อผู้ใช้")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณากรอกรหัสผ่าน")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "รหัสผ่านต้องมีอย่างน้อย 6 ตัวอักษร")]
        [Display(Name = "รหัสผ่าน")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณายืนยันรหัสผ่าน")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "รหัสผ่านไม่ตรงกัน")]
        [Display(Name = "ยืนยันรหัสผ่าน")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณาเลือกเพศ")]
        [Display(Name = "เพศ")]
        public string Gender { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณากรอกเบอร์โทรศัพท์")]
        [Phone(ErrorMessage = "รูปแบบเบอร์โทรไม่ถูกต้อง")]
        [Display(Name = "เบอร์โทรศัพท์")]
        public string Phone { get; set; } = string.Empty;
    }

    // ============================================================
    // Model สำหรับ AddUser (Admin เพิ่มผู้ใช้)
    // ============================================================
    public class AddUserViewModel
    {
        [Required(ErrorMessage = "กรุณากรอกชื่อ")]
        [Display(Name = "ชื่อ")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณากรอกนามสกุล")]
        [Display(Name = "นามสกุล")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณากรอกอีเมล")]
        [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
        [Display(Name = "อีเมล")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณากรอกชื่อผู้ใช้")]
        [Display(Name = "ชื่อผู้ใช้")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณากรอกรหัสผ่าน")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "รหัสผ่านต้องมีอย่างน้อย 6 ตัวอักษร")]
        [Display(Name = "รหัสผ่าน")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณาเลือกบทบาท")]
        [Display(Name = "บทบาท")]
        public string Role { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณาเลือกเพศ")]
        [Display(Name = "เพศ")]
        public string Gender { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณากรอกเบอร์โทรศัพท์")]
        [Phone(ErrorMessage = "รูปแบบเบอร์โทรไม่ถูกต้อง")]
        [Display(Name = "เบอร์โทรศัพท์")]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "สถานะ")]
        public bool IsActive { get; set; } = true;
    }

    // ============================================================
    // Model สำหรับแสดงรายการ User ใน UserList
    // ============================================================
    public class UserListItem
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
