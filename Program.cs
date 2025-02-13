using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using WebApplication1.Model;
using WebApplication1.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTransient<IEmailSender, EmailSender>();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddHttpClient();
builder.Services.AddDbContext<AuthDbContext>();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AuthDbContext>()
    .AddDefaultTokenProviders();

// ✅ Register CustomUserManager properly
builder.Services.AddScoped<CustomUserManager>();

// ✅ Ensure Identity uses the correct user manager
builder.Services.AddScoped<UserManager<ApplicationUser>>(provider =>
    provider.GetRequiredService<CustomUserManager>()
);

builder.Services.AddAuthentication("MyCookieAuth").AddCookie("MyCookieAuth", options =>
{
    options.Cookie.Name = "MyCookieAuth";
    options.Cookie.HttpOnly = true;  // ⛔ Prevent JavaScript from accessing the cookie
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // 🔒 Force HTTPS
    options.Cookie.SameSite = SameSiteMode.Strict; // 🔒 Prevent CSRF attacks
    options.ExpireTimeSpan = TimeSpan.FromMinutes(20); // ⏳ Cookie expiration time
    options.SlidingExpiration = true; // ⏳ Refresh session expiration on activity
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("MustBelongToHRDepartment",
        policy => policy.RequireClaim("Department", "HR"));
});

builder.Services.ConfigureApplicationCookie(Config =>
{
    Config.LoginPath = "/Login";
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(5); // ⏳ Set session expiration time
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddDistributedMemoryCache(); // Required for session state

builder.Services.Configure<IdentityOptions>(options =>
{
    // ✅ Enable account lockout
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(1); // 1-minute lockout
    options.Lockout.MaxFailedAccessAttempts = 3; // Lock account after 3 failed attempts
    options.Lockout.AllowedForNewUsers = true; // Apply lockout even for new users
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseStatusCodePagesWithRedirects("/errors/{0}");
app.UseRouting();

app.UseSession(); // ✅ Ensure session is initialized before authentication
app.UseAuthentication();
app.UseAuthorization();

// ✅ Ensure only one active session per user
app.Use(async (context, next) =>
{
    var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
    var signInManager = context.RequestServices.GetRequiredService<SignInManager<ApplicationUser>>();

    if (context.User.Identity.IsAuthenticated && context.Session != null)
    {
        var user = await userManager.GetUserAsync(context.User);
        if (user != null)
        {
            var sessionId = context.Session.GetString("SessionId");

            // ✅ If session ID does not exist in DB, create a new one
            if (string.IsNullOrEmpty(user.SessionId))
            {
                user.SessionId = sessionId;
                await userManager.UpdateAsync(user);
            }

            // ✅ If Session ID does not match the stored one, log out previous sessions
            if (user.SessionId != sessionId)
            {
                await signInManager.SignOutAsync();
                context.Response.Redirect("/Login");
                return;
            }
        }
    }

    await next();
});

app.MapRazorPages();
app.Run();
