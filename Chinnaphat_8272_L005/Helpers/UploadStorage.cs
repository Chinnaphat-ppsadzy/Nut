using Microsoft.AspNetCore.Hosting;

namespace Chinnaphat_8272_L005.Helpers;

/// <summary>
/// Keeps user-uploaded files outside wwwroot so dotnet watch does not restart
/// the application when a customer or staff member uploads a slip.
/// </summary>
public static class UploadStorage
{
    public static string GetSlipDirectory(IWebHostEnvironment environment) =>
        Path.Combine(environment.ContentRootPath, "App_Data", "uploads", "slips");

    public static string GetSlipPath(IWebHostEnvironment environment, string slipUrl)
    {
        var fileName = Path.GetFileName(slipUrl);
        var currentPath = Path.Combine(GetSlipDirectory(environment), fileName);
        if (File.Exists(currentPath)) return currentPath;

        // Keep slips uploaded before this storage change available for review.
        var legacyDirectory = Path.Combine(environment.WebRootPath, "uploads", "slips");
        return Path.Combine(legacyDirectory, fileName);
    }
}
