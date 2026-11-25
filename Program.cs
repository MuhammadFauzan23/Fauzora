using Fauzora.Data;
using Fauzora.Models;
using Fauzora.Repositories;
using Fauzora.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.Features;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// 1. Registrasi MVC, database, Identity, repository, service, dan worker.
builder.Services.AddControllersWithViews();
builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = 52L * 1024 * 1024);
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection belum dikonfigurasi. Atur melalui User Secrets atau environment variable.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
    options.Lockout.AllowedForNewUsers = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    // Identity menyimpan tiket login di cookie terenkripsi, bukan di HttpContext.Session.
    options.Cookie.Name = "Fauzora.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    var identityValidation = options.Events.OnValidatePrincipal;
    options.Events.OnValidatePrincipal = async context =>
    {
        if (identityValidation is not null)
            await identityValidation(context);
        if (context.Principal?.Identity?.IsAuthenticated != true) return;

        var userId = context.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            context.RejectPrincipal();
            return;
        }

        await using var scope = context.HttpContext.RequestServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var account = await db.Users.AsNoTracking().Where(user => user.Id == userId)
            .Select(user => new { user.IsActive, user.SecurityStamp })
            .FirstOrDefaultAsync(context.HttpContext.RequestAborted);
        if (account is null || !account.IsActive)
        {
            context.RejectPrincipal();
            return;
        }

        var stampClaimType = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<IdentityOptions>>().Value
            .ClaimsIdentity.SecurityStampClaimType;
        var cookieSecurityStamp = context.Principal.FindFirstValue(stampClaimType);
        if (string.IsNullOrWhiteSpace(cookieSecurityStamp) || cookieSecurityStamp != account.SecurityStamp)
        {
            context.RejectPrincipal();
            return;
        }

        var databaseRoles = await (from userRole in db.UserRoles.AsNoTracking()
                                   join role in db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                                   where userRole.UserId == userId
                                   select role.Name!).ToListAsync(context.HttpContext.RequestAborted);
        var cookieRoles = context.Principal.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToHashSet(StringComparer.Ordinal);
        if (!cookieRoles.SetEquals(databaseRoles))
            context.RejectPrincipal();
    };
});

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "Fauzora.AntiForgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

builder.Services.AddScoped<ProductRepository>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<ProductImageStorageService>();
builder.Services.AddScoped<OrderRepository>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<CartRepository>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<CategoryRepository>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<ReviewRepository>();
builder.Services.AddScoped<ReviewService>();
builder.Services.AddSingleton<RichTextSanitizer>();
builder.Services.AddSingleton<SecureIdService>();
builder.Services.AddScoped<AdminUserManagementService>();
builder.Services.AddHostedService<OrderAutoCompletionService>();

var app = builder.Build();

// 2. Middleware memproses request: error handling, file statis, routing, login, lalu authorization.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    await next();
});
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    // 3. Migration dan seed role/akun dijalankan sebelum server menerima request.
    await DbInitializer.SeedAsync(scope.ServiceProvider);
}

app.Run();
