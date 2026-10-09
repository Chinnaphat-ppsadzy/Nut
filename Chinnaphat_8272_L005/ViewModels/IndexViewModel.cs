namespace Chinnaphat_8272_L005.ViewModels
{
    // ViewModel สำหรับหน้า Index (แสดงข้อมูลผู้ที่ล็อกอินสำเร็จ)
    public class IndexViewModel
    {
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsLoggedIn { get; set; } = false;
        public DateTime LoginTime { get; set; } = DateTime.Now;
    }
}
