using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Spirometry.Importer.Persistence.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Spirometry.Importer.Persistence.Configuration
{
    public sealed class ImportLogConfiguration
       : IEntityTypeConfiguration<ImportLogEntity>
    {
        
        public void Configure(EntityTypeBuilder<ImportLogEntity> builder)
        {
            
            builder.ToTable("import_logs");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            builder.Property(x => x.RunId).HasColumnName("run_id").IsRequired();
            builder.Property(x => x.FileName).HasColumnName("file_name").HasMaxLength(255).IsRequired();
            builder.Property(x => x.TenantId).HasColumnName("tenant_id").HasMaxLength(50).IsRequired(false);
            builder.Property(x => x.OrderId).HasColumnName("order_id").HasMaxLength(50).IsRequired(false);
            builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
            builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(100).IsRequired();
            builder.Property(x => x.Warnings).HasColumnName("warnings").HasColumnType("text[]").IsRequired();
            builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").IsRequired();

            builder.HasIndex(x => x.RunId);
            builder.HasIndex(x => new { x.TenantId, x.CreatedAtUtc });
        }
    }
}
