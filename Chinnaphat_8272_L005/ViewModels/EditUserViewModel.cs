using System.ComponentModel.DataAnnotations;

namespace Chinnaphat_8272_L005.ViewModels
{
    // ============================================================
    // ViewModel สำหรับ EditUser (UPDATE) — Password ไม่ Required
    // เพราะถ้าไม่กรอกหมายถึงไม่ต้องการเปลี่ยนรหัสผ่าน
    // ============================================================
    public class EditUserViewModel
    {
        // UserId สำหรับระบุว่าแก้ไข User คนไหน
        public int UserId { get; set; }

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

        [Phone(ErrorMessage = "รูปแบบเบอร์โทรไม่ถูกต้อง")]
        [Display(Name = "เบอร์โทรศัพท์")]
        public string? Phone { get; set; }

        // Password เป็น optional — ถ้าเว้นว่างไว้จะไม่เปลี่ยน
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "รหัสผ่านต้องมีอย่างน้อย 6 ตัวอักษร")]
        [Display(Name = "รหัสผ่านใหม่")]
        public string? Password { get; set; }

        [Display(Name = "บทบาท")]
        public string? Role { get; set; }

        [Display(Name = "สถานะ")]
        public bool IsActive { get; set; } = true;
    }
}
