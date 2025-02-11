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
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.Run();
