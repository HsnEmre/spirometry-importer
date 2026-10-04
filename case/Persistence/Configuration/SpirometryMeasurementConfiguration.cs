using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Spirometry.Importer.Persistence.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Spirometry.Importer.Persistence.Configuration
{
    public sealed class SpirometryMeasurementConfiguration
        : IEntityTypeConfiguration<SpirometryMeasurementEntity>
    {
        
        public void Configure(EntityTypeBuilder<SpirometryMeasurementEntity> builder)
        {
          
            builder.ToTable("spirometry_measurements");
            builder.HasKey(x => new { x.TestId, x.Code });
            builder.Property(x => x.TenantId).HasColumnName("tenant_id").HasMaxLength(50).IsRequired();
            builder.Property(x => x.TestId).HasColumnName("test_id").ValueGeneratedNever();
            builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
            builder.Property(x => x.Value).HasColumnName("value").HasColumnType("numeric").IsRequired();
            builder.Property(x => x.Unit).HasColumnName("unit").HasMaxLength(30).IsRequired();
            builder.Property(x => x.PredictedValue).HasColumnName("predicted_value").HasColumnType("numeric").IsRequired(false);
            builder.Property(x => x.LowerLimitOfNormal).HasColumnName("lower_limit_of_normal").HasColumnType("numeric").IsRequired(false);

           
            builder.HasOne<SpirometryTestEntity>()               
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.TestId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
