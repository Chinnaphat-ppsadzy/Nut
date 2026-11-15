namespace Chinnaphat_8272_L005.Helpers
{
    /// <summary>
    /// Extension methods สำหรับอ่านข้อมูล Session ของผู้ใช้ที่ Login อยู่
    /// </summary>
    public static class SessionHelper
    {
        public static int? GetUserId(this ISession session)
            => session.GetInt32("UserId");

        public static string? GetRoleCode(this ISession session)
            => session.GetString("RoleCode");

        public static string? GetFullName(this ISession session)
            => session.GetString("FullName");

        public static string? GetEmail(this ISession session)
            => session.GetString("Email");

        public static bool IsLoggedIn(this ISession session)
            => session.GetInt32("UserId") != null;

        public static bool IsInRole(this ISession session, params string[] roles)
        {
            var code = session.GetString("RoleCode");
            return !string.IsNullOrEmpty(code) && roles.Contains(code);
        }
    }
}
