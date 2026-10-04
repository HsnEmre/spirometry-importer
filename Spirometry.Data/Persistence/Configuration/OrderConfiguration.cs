using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Spirometry.Importer.Persistence.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Spirometry.Importer.Persistence.Configuration
{
    public sealed class OrderConfiguration : IEntityTypeConfiguration<OrderEntity>
    {
        public void Configure(EntityTypeBuilder<OrderEntity> builder)
        {
            builder.ToTable("orders");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasMaxLength(50)
                .ValueGeneratedNever();

            builder.Property(x => x.TenantId)
                .HasColumnName("tenant_id")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.EmployeeRef)
                .HasColumnName("employee_ref")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.OrderDate)
                .HasColumnName("order_date")
                .HasColumnType("date")
                .IsRequired();

            //testlerin iş emri kiracıyla birlikte bağlabilmesi için alternatif anahat
            builder.HasAlternateKey(x => new { x.TenantId, x.Id });

            builder.HasOne<TenantEntity>()
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
