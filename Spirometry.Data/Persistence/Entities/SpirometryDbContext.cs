using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Spirometry.Importer.Persistence.Entities
{
    public sealed class SpirometryDbContext : DbContext
    {
        public SpirometryDbContext(DbContextOptions<SpirometryDbContext> options) : base(options)
        { }
        public DbSet<TenantEntity> Tenants => Set<TenantEntity>();
        public DbSet<OrderEntity> Orders => Set<OrderEntity>();
        public DbSet<SpirometryTestEntity> SpirometryTests => Set<SpirometryTestEntity>();
        public DbSet<SpirometryMeasurementEntity> SpirometryMeasurements =>
            Set<SpirometryMeasurementEntity>();

        public DbSet<ImportLogEntity> ImportLogs => Set<ImportLogEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Temel sınıfın model oluşturma davranışını çağırır.
            base.OnModelCreating(modelBuilder);

            // Bu assembly içindeki entity yapılandırmalarını bulup uygular.
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(SpirometryDbContext).Assembly);
        }
    }
}


