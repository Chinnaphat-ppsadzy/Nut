using Microsoft.EntityFrameworkCore;
using Chinnaphat_8272_L005.Models;
using Chinnaphat_8272_L005.Database;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddDbContext<Csi402dbContext>(
    options => options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    )
);

var app = builder.Build();

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
app.UseRouting();
app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Customer}/{id?}")
    .WithStaticAssets();


app.Run();
