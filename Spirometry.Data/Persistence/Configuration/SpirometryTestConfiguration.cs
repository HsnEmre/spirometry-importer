using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Spirometry.Importer.Persistence.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Spirometry.Importer.Persistence.Configuration
{
    public sealed class SpirometryTestConfiguration : IEntityTypeConfiguration<SpirometryTestEntity>
    {
        public void Configure(EntityTypeBuilder<SpirometryTestEntity> builder)
        {
            builder.ToTable("spirometry_tests");
            builder.HasKey(x => x.Id);
            builder.Property(x=>x.Id).HasColumnName("id").ValueGeneratedNever();
            builder.Property(x => x.TenantId).HasColumnName("tenant_id").HasMaxLength(50).IsRequired();
            builder.Property(x => x.OrderId).HasColumnName("order_id").HasMaxLength(50).IsRequired();
            builder.Property(x => x.DevicePatientId).HasColumnName("device_patient_id").HasMaxLength(100).IsRequired();
            builder.Property(x => x.TestType).HasColumnName("test_type").HasMaxLength(30).IsRequired();
            builder.Property(x => x.TestDate).HasColumnName("test_date").HasColumnType("timestamp without time zone").IsRequired();
            builder.Property(x => x.QualityGrade).HasColumnName("quality_grade").HasMaxLength(20).IsRequired(false);
            builder.Property(x => x.SourceFileName).HasColumnName("source_file_name").HasMaxLength(255).IsRequired();
            builder.Property(x => x.ContentHash).HasColumnName("content_hash").HasMaxLength(64).IsRequired();
            builder.Property(x => x.Warnings).HasColumnName("warnings").HasColumnType("text[]").IsRequired();
            builder.Property(x => x.ImportedAtUtc).HasColumnName("imported_at_utc").HasColumnType("timestamp with time zone").IsRequired();
            builder.HasAlternateKey(x => new { x.TenantId, x.Id });
            builder.HasIndex(x => new
            {
                x.TenantId,
                x.OrderId,
                x.DevicePatientId,
                x.TestDate,
                x.TestType
            }).IsUnique()
            .HasDatabaseName("ux_spirometry_tests_identity");

            builder.HasOne<OrderEntity>()
               .WithMany()
               .HasForeignKey(x => new { x.TenantId, x.OrderId })
               .HasPrincipalKey(x => new { x.TenantId, x.Id })
               .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
