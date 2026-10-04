using Spirometry.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Spirometry.Web.Services;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddControllersWithViews();


// Sunucu belleğinde süreli kayıt tutulmasını sağlar.
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<DatabaseSessionStore, DatabaseSessionStore>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.Cookie.Name = "Spirometry.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        // Siteler arası isteklerde çerezin gönderimini sınırlar.
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = DatabaseSessionStore.SessionLigeTime;
        options.SlidingExpiration = false;
        options.Events.OnValidatePrincipal = async context =>
        {

            string? sessionId =
                context.Principal?.FindFirst("db_session_id")?.Value;
            var sessions = context.HttpContext.RequestServices
                .GetRequiredService<DatabaseSessionStore>();

            if (sessions.Get(sessionId) is null)
            {
                context.RejectPrincipal();

                await context.HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });

builder.Services.AddScoped<WebDbContextFactory>();

var app = builder.Build();



// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
