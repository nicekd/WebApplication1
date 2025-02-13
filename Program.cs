﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using WebApplication1.Model;
using WebApplication1.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTransient<IEmailSender, EmailSender>();

builder.Services.AddScoped<IAuditLogService, AuditLogService>();

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
    options.Lockout.MaxFailedAccessAttempts = 4; // Lock account after 3 failed attempts
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
app.Use(async (context, next) =>
{
    await next();

    if (context.Response.StatusCode == 404)
    {
        context.Response.Redirect("/errors/404");
    }
    else if (context.Response.StatusCode == 403)
    {
        context.Response.Redirect("/errors/403");
    }
    else if (context.Response.StatusCode == 500)
    {
        context.Response.Redirect("/errors/500");
    }
});

// ✅ Basic Global Exception Handling - Redirects to /errors/500
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch
    {
        context.Response.Redirect("/errors/500");
    }
});

app.UseRouting();

app.UseSession(); // ✅ Ensure session is initialized before authentication
app.UseAuthentication();
app.UseAuthorization();

// ✅ Ensure only one active session per user and log user actions
app.Use(async (context, next) =>
{
    var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
    var signInManager = context.RequestServices.GetRequiredService<SignInManager<ApplicationUser>>();
    var dbContext = context.RequestServices.GetRequiredService<AuthDbContext>();

    if (context.User.Identity.IsAuthenticated && context.Session != null)
    {
        var user = await userManager.GetUserAsync(context.User);
        if (user != null)
        {
            var sessionId = context.Session.GetString("SessionId");
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            // ✅ If session ID does not exist in DB, create a new one
            if (string.IsNullOrEmpty(user.SessionId))
            {
                user.SessionId = sessionId;
                await userManager.UpdateAsync(user);

                // ✅ Log login event
                dbContext.AuditLogs.Add(new AuditLog
                {
                    UserId = user.Id,
                    Action = "Login",
                    Timestamp = DateTime.UtcNow,
                    IPAddress = ipAddress
                });
                await dbContext.SaveChangesAsync();
            }

            // ✅ If session ID does not match, log out previous session
            if (user.SessionId != sessionId)
            {
                // ✅ Log logout event due to session conflict
                dbContext.AuditLogs.Add(new AuditLog
                {
                    UserId = user.Id,
                    Action = "Logged Out (Session Conflict)",
                    Timestamp = DateTime.UtcNow,
                    IPAddress = ipAddress
                });
                await dbContext.SaveChangesAsync();

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
