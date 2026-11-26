using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Chinnaphat_8272_L005.Models;
using Chinnaphat_8272_L005.Database;
using Chinnaphat_8272_L005.Helpers;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDataProtection();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.Name = "CourtBook.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddDbContext<Csi402dbContext>(
    options => options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    )
);

var app = builder.Build();

var thaiCulture = CultureInfo.GetCultureInfo("th-TH");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(thaiCulture),
    SupportedCultures = new[] { thaiCulture },
    SupportedUICultures = new[] { thaiCulture }
});

if (app.Environment.IsDevelopment())
{
    using var schemaScope = app.Services.CreateScope();
    var db = schemaScope.ServiceProvider.GetRequiredService<Csi402dbContext>();
    try
    {
        await WalkInPaymentSchemaInitializer.EnsureColumnsAsync(db);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Could not prepare the Walk-in payment columns. Apply Database/WalkInPayments.sql to the configured database.");
    }
}

using (var promoSchemaScope = app.Services.CreateScope())
{
    var db = promoSchemaScope.ServiceProvider.GetRequiredService<Csi402dbContext>();
    try
    {
        await WalkInPaymentSchemaInitializer.EnsurePromotionDaysColumnAsync(db);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Could not prepare the active_days column for time-based promotions.");
    }
}

using (var issueSchemaScope = app.Services.CreateScope())
{
    var db = issueSchemaScope.ServiceProvider.GetRequiredService<Csi402dbContext>();
    try
    {
        await WalkInPaymentSchemaInitializer.EnsureCourtIssueBlockColumnAsync(db);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Could not prepare the court issue maintenance block column.");
    }
}

using (var rentalRefundSchemaScope = app.Services.CreateScope())
{
    var db = rentalRefundSchemaScope.ServiceProvider.GetRequiredService<Csi402dbContext>();
    try
    {
        await WalkInPaymentSchemaInitializer.EnsureRentalDepositRefundTableAsync(db);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Could not prepare the rental deposit refund request table.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    // Slip files must only be served through the owner/staff authorization action.
    if (context.Request.Path.StartsWithSegments("/uploads/slips", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    await next();
});
// Uploaded product images are created at runtime, so serve them from wwwroot
// in addition to the build-time static asset manifest below.
app.UseStaticFiles();
// Serve product images from App_Data so runtime uploads are not watched as wwwroot
// content files by dotnet watch, which would restart the app after each upload.
var productImageDirectory = Path.Combine(app.Environment.ContentRootPath, "App_Data", "uploads", "products");
Directory.CreateDirectory(productImageDirectory);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(productImageDirectory),
    RequestPath = "/uploads/products"
});
app.UseRouting();
app.UseSession();
app.UseMiddleware<RememberMeSessionMiddleware>();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
