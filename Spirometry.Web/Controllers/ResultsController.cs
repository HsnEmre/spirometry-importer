using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Spirometry.Web.Models;
using Spirometry.Web.Services;

namespace Spirometry.Web.Controllers
{
    [Authorize]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public sealed class ResultsController : Controller
    {
        private readonly DatabaseSessionStore _sessions;

        private readonly WebDbContextFactory _dbFactory;

        public ResultsController(DatabaseSessionStore sessions, WebDbContextFactory dbFactory)
        {
            _sessions = sessions;
            _dbFactory = dbFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            string? sessionId = User.FindFirst("db_session_id")?.Value;

            DatabaseSession? session = _sessions.Get(sessionId);
            if (session is null)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);//artık kullanılmayan giriş çerezini kaldırı
                return RedirectToAction("Login", "Account");
            }

            try
            {
                await using var db = _dbFactory.Create(session);

                var tests = await (
                    from test in db.SpirometryTests.AsNoTracking()
                    join order in db.Orders.AsNoTracking()
                    on new
                    {
                        test.TenantId,
                        OrderId = test.OrderId
                    }
                    equals new
                    {
                        order.TenantId,
                        OrderId = order.Id
                    }
                    orderby test.TestDate descending, test.Id
                    select new TestListItemViewModel
                    {
                        Id = test.Id,
                        TenantId = test.TenantId,
                        OrderId = test.OrderId,
                        EmployeeRef = order.EmployeeRef,
                        TestDate = test.TestDate,
                        TestType = test.TestType,
                        QualityGrade = test.QualityGrade,
                        Warnings = test.Warnings
                    }).ToListAsync(HttpContext.RequestAborted);

                return View(tests);
            }
            catch (Exception ex) when (
               ex is NpgsqlException or TimeoutException)
            {

                ViewData["LoadError"] =
                     "Sonuçlar okunamadı. Bağlantıyı kontrol edip tekrar deneyin.";

                Response.StatusCode = StatusCodes.Status503ServiceUnavailable;

                return View(new List<TestListItemViewModel>());


            }

        }


        #region helper
        
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
           
            string? sessionId = User.FindFirst("db_session_id")?.Value;

            DatabaseSession? session = _sessions.Get(sessionId);

            if (session is null)
            {
                // Geçersiz kalan giriş çerezini kaldırır.
                await HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);

                return RedirectToAction("Login", "Account");
            }

            try
            {
                // Giriş yapan OSGB hesabıyla context oluşturur.
                await using var db = _dbFactory.Create(session);

                var test = await (
                    from item in db.SpirometryTests.AsNoTracking()
                    join order in db.Orders.AsNoTracking()
                        on new
                        {
                            item.TenantId,
                            OrderId = item.OrderId
                        }
                        equals new
                        {
                            order.TenantId,
                            OrderId = order.Id
                        }
                    where item.Id == id
                    select new TestListItemViewModel
                    {
                        Id = item.Id,
                        TenantId = item.TenantId,
                        OrderId = item.OrderId,
                        EmployeeRef = order.EmployeeRef,
                        TestDate = item.TestDate,
                        TestType = item.TestType,
                        QualityGrade = item.QualityGrade,
                        Warnings = item.Warnings
                    })
                    .SingleOrDefaultAsync(HttpContext.RequestAborted);

                if (test is null)
                {
                    return NotFound();
                }

                // Görülebilen teste ait ölçümleri getirir.
                var measurements = await db.SpirometryMeasurements
                    .AsNoTracking()
                    .Where(item => item.TestId == id)
                    .OrderBy(item => item.Code)
                    .Select(item => new MeasurementViewModel
                    {
                        Code = item.Code,
                        Value = item.Value,
                        Unit = item.Unit,
                        PredictedValue = item.PredictedValue,
                        LowerLimitOfNormal = item.LowerLimitOfNormal
                    })
                    .ToListAsync(HttpContext.RequestAborted);

                var model = new TestDetailsViewModel
                {
                    Test = test,
                    Measurements = measurements
                };

                return View(model);
            }
            catch (Exception ex) when (
                ex is NpgsqlException or TimeoutException)
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    "Test detayi okunamadi. Lutfen tekrar deneyin.");
            }
        }
        #endregion
    }




}

