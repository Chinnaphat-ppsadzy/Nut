using System.ComponentModel.DataAnnotations;

namespace Chinnaphat_8272_L005.ViewModels;

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "กรุณากรอกอีเมล")]
    [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
    [Display(Name = "อีเมล")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกเบอร์โทรศัพท์ที่ผูกกับบัญชี")]
    [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "กรุณากรอกเบอร์โทรศัพท์ 10 หลัก โดยไม่ใส่ขีด")]
    [Display(Name = "เบอร์โทรศัพท์")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากำหนดรหัสผ่านใหม่")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "รหัสผ่านต้องมีอย่างน้อย 6 ตัวอักษร")]
    [DataType(DataType.Password)]
    [Display(Name = "รหัสผ่านใหม่")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณายืนยันรหัสผ่านใหม่")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "รหัสผ่านไม่ตรงกัน")]
    [Display(Name = "ยืนยันรหัสผ่านใหม่")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
