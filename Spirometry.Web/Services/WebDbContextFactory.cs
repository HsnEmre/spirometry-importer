using Microsoft.EntityFrameworkCore;
using Npgsql;
using Spirometry.Importer.Persistence.Entities;

namespace Spirometry.Web.Services
{
    public sealed class WebDbContextFactory
    {
        public SpirometryDbContext Create(DatabaseSession session)
        {
            bool validAccount = (session.TenantId, session.Username) switch
            {
                ("T-01", "spirometry_osgb_a") => true,
                ("T-02", "spirometry_osgb_b") => true,
                _ => false
            };

            if (!validAccount)
            {
                throw new InvalidOperationException(
                   "Gecersiz OSGB hesap eslesmesi.");
            }

            var connectionSettings = new NpgsqlConnectionStringBuilder
            {
                Host = "localhost",
                Port = 5432,
                Database = "spirometry_db",
                Username = session.Username,
                Password = session.Password,

                // Demo uygulamasında bağlantı havuzunu kapalı tutar.
                Pooling = false,

                // Bağlantı için bekleme süresini sınırlar.
                Timeout = 10
            };
            var options = new DbContextOptionsBuilder<SpirometryDbContext>()
               .UseNpgsql(connectionSettings.ConnectionString)
               .Options;

            return new SpirometryDbContext(options);

        }
    }
}
