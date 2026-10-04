using Microsoft.EntityFrameworkCore;
using Npgsql;
using Spirometry.Importer.Persistence.Entities;
using Spirometry.Viewer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Spirometry.Viewer.Services
{
    public sealed class ResultReader
    {
        // Bu okuyucuya ait bağlantı ayarlarını tutar.
        private readonly DbContextOptions<SpirometryDbContext> _options;

        // OSGB hesabı ve parolasıyla okuyucuyu hazırlar.
        public ResultReader(string username, string password)
        {
            // Ekranın yalnızca iki sınırlı demo hesabını kullanmasına izin verir.
            if (username != "spirometry_osgb_a" &&
                username != "spirometry_osgb_b")
            {
                // Yönetici veya import hesabıyla bağlantı kurulmasını engeller.
                throw new ArgumentException("Gecersiz OSGB hesabi.");
            }

            // Boş parola gönderilmesini engeller.
            if (string.IsNullOrWhiteSpace(password))
            {
                // Kullanıcıya parola gerektiğini bildirir.
                throw new ArgumentException("Hesap parolasini girin.");
            }

            // Seçilen OSGB hesabının bağlantı bilgilerini hazırlar.
            var connectionBuilder = new NpgsqlConnectionStringBuilder
            {
                // Yerel PostgreSQL sunucusunu kullanır.
                Host = "localhost",

                // PostgreSQL bağlantı noktasını belirtir.
                Port = 5432,

                // Sonuçların saklandığı veritabanını seçer.
                Database = "spirometry_db",

                // Ekranda seçilen sınırlı hesabı kullanır.
                Username = username,

                // Kullanıcının girdiği parolayı kullanır.
                Password = password,

                // Bu küçük demoda işlemler arasında bağlantı havuzu kullanmaz.
                Pooling = false
            };

            // Context ayarlarını oluşturur.
            var optionsBuilder =
                new DbContextOptionsBuilder<SpirometryDbContext>();

            // PostgreSQL bağlantısını tanımlar.
            optionsBuilder.UseNpgsql(connectionBuilder.ConnectionString);

            // Sonraki okuma işlemlerinde kullanılacak ayarları saklar.
            _options = optionsBuilder.Options;
        }

        // Hesabın erişebildiği testleri çalışan referanslarıyla getirir.
        public async Task<List<TestSummary>> GetTestsAsync()
        {
            // Bu sorguya özel context oluşturur ve işlem sonunda kapatır.
            await using var db = new SpirometryDbContext(_options);

            // Testleri kendi iş emirleriyle ilişkilendiren LINQ sorgusunu kurar.
            var query =
                // Test kayıtlarını değişiklik takibi olmadan okur.
                from test in db.SpirometryTests.AsNoTracking()

                    // İş emirlerini değişiklik takibi olmadan sorguya dahil eder.
                join order in db.Orders.AsNoTracking()

                // Eşleştirmede hem kiracıyı hem iş emri kimliğini kullanır.
                on new { test.TenantId, OrderId = test.OrderId }
                equals new { order.TenantId, OrderId = order.Id }

                // Son testleri önce gösterir; eşit tarihlerde kimliğe göre sıralar.
                orderby test.TestDate descending, test.Id

                // Veritabanındaki alanları ekran modeline aktarır.
                select new TestSummary
                {
                    // Detay sorgusunda kullanılacak test kimliğini alır.
                    Id = test.Id,

                    // OSGB kimliğini alır.
                    TenantId = test.TenantId,

                    // İş emri kimliğini alır.
                    OrderId = test.OrderId,

                    // Çalışan referansını iş emrinden alır.
                    EmployeeRef = order.EmployeeRef,

                    // Test tarihini alır.
                    TestDate = test.TestDate,

                    // Test türünü alır.
                    TestType = test.TestType,

                    // Kalite derecesini alır.
                    QualityGrade = test.QualityGrade,

                    // Eksiklik uyarılarını alır.
                    Warnings = test.Warnings
                };

            // Sorguyu PostgreSQL'de çalıştırıp sonuçları listeye dönüştürür.
            return await query.ToListAsync();
        }

        // Seçilen testin erişilebilir ölçümlerini getirir.
        public async Task<List<SpirometryMeasurementEntity>> GetMeasurementsAsync(
            // Ekranda seçilen testin kimliğini alır.
            Guid testId)
        {
            // Ölçüm sorgusu için ayrı context oluşturur.
            await using var db = new SpirometryDbContext(_options);

            // Ölçüm sorgusunu oluşturup çalıştırır.
            return await db.SpirometryMeasurements
                // Görüntüleme işlemi olduğu için değişiklik takibini kapatır.
                .AsNoTracking()
                // Yalnızca seçilen testin ölçümlerini ister.
                .Where(measurement => measurement.TestId == testId)
                // Ölçümleri kodlarına göre sıralar.
                .OrderBy(measurement => measurement.Code)
                // Sonuçları asenkron olarak listeye getirir.
                .ToListAsync();
        }
    }
}
