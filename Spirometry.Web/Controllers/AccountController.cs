using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Spirometry.Web.Models;
using Spirometry.Web.Services;
using System.Security.Claims;

namespace Spirometry.Web.Controllers
{
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public sealed class AccountController : Controller
    {

        private readonly DatabaseSessionStore _sessions;
        public AccountController(DatabaseSessionStore sessions)
        {
            _sessions = sessions;
        }


        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            // Zaten giriş yapılmışsa ana sayfaya gönderir.
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Results");
            }

            // Boş giriş formunu gösterir.
            return View(new LoginViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            // Mevcut oturumu değiştirmek için önce çıkış yapılmasını ister.
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            // Zorunlu alanları sunucu tarafında kontrol eder.
            if (!ModelState.IsValid)
            {
                return LoginError(model);
            }

            // Yalnızca izin verdiğimiz iki hesabı seçilebilir yapar.
            string? username = model.TenantCode switch
            {
                "T-01" => "spirometry_osgb_a",
                "T-02" => "spirometry_osgb_b",
                _ => null
            };

            // Form dışarıdan değiştirilmişse bilinmeyen OSGB'yi reddeder.
            if (username is null)
            {
                ModelState.AddModelError(
                    nameof(model.TenantCode), "Geçerli bir OSGB seçin.");

                return LoginError(model);
            }

            // Bağlantı bilgisini güvenli biçimde oluşturur.
            var connectionSettings = new NpgsqlConnectionStringBuilder
            {
                Host = "localhost",
                Port = 5432,
                Database = "spirometry_db",
                Username = username,
                Password = model.Password,

                // Giriş kontrolünde önceki bir bağlantı kullanılmasını engeller.
                Pooling = false,

                // Bağlantı kurulması için en fazla 10 saniye bekler.
                Timeout = 10
            };

            try
            {
                // Seçilen sınırlı OSGB hesabıyla bağlantı oluşturur.
                await using var connection =
                    new NpgsqlConnection(connectionSettings.ConnectionString);

                // PostgreSQL hesabın giriş bilgilerini doğrular.
                await connection.OpenAsync();
            }
            catch (PostgresException ex) when (
                ex.SqlState is "28P01" or "28000")
            {
                // Parolayı veya bağlantı ayrıntılarını göstermeden bilgi verir.
                ModelState.AddModelError(
                    string.Empty, "OSGB hesabı veya parola doğrulanamadı.");

                return LoginError(model);
            }
            catch (Exception ex) when (
                ex is NpgsqlException or TimeoutException)
            {
                // Bağlantı hatasında oturum oluşturmaz.
                ModelState.AddModelError(
                    string.Empty, "Veritabanına bağlanılamadı.");

                return LoginError(model);
            }

            // Başarıyla doğrulanan hesabı sunucu belleğinde saklar.
            string sessionId = _sessions.Create(new DatabaseSession
            {
                TenantId = model.TenantCode,
                Username = username,
                Password = model.Password
            });

            // Çereze parola yerine yalnızca oturum kimliğini koyar.
            var claims = new[]
            {
                new Claim("db_session_id", sessionId)
            };

            // Kimliği cookie doğrulama yöntemiyle ilişkilendirir.
            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            // Kullanıcının giriş kimliğini oluşturur.
            var principal = new ClaimsPrincipal(identity);

            // Giriş çerezini oluşturur.
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    // Kalıcı bir "beni hatırla" çerezi oluşturmaz.
                    IsPersistent = false,

                    // Girişin geçerlilik süresini sınırlar.
                    ExpiresUtc = DateTimeOffset.UtcNow.Add(
                        DatabaseSessionStore.SessionLigeTime),

                    // Otomatik yenilemeyi kapatır.
                    AllowRefresh = false
                });

            // Başarılı girişten sonra mevcut ana sayfaya gider.
            return RedirectToAction("Index", "Results");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Kullanıcının oturum kimliğini alır.
            string? sessionId = User.FindFirst("db_session_id")?.Value;

            // Sunucuda saklanan bağlantı bilgisini kaldırır.
            _sessions.Remove(sessionId);

            // Tarayıcıdaki giriş çerezini kaldırır.
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            // Kullanıcıyı giriş ekranına gönderir.
            return RedirectToAction(nameof(Login));
        }

        // Hatalı girişte parolayı yeniden forma taşımadan ekranı gösterir.
        private IActionResult LoginError(LoginViewModel model)
        {
            // Modeldeki parolayı temizler.
            model.Password = string.Empty;

            // Doğrulama hatalarını koruyarak gönderilmiş parola değerini siler.
            ModelState.SetModelValue(
                nameof(LoginViewModel.Password), string.Empty, string.Empty);

            // Hata mesajlarıyla giriş formunu tekrar gösterir.
            return View("Login", model);
        }

    }
}
