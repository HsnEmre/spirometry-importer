using System;
using System.Collections.Generic;
using System.Text;

namespace Spirometry.Importer.Persistence.Entities
{
    public sealed class SpirometryTestEntity
    {
        // Kayda uygulama tarafından benzersiz bir kimlik verir.
        public Guid Id { get; set; } = Guid.NewGuid();

        // Testin ait olduğu OSGB'yi tutar; iş emrinden belirlenecek.
        public required string TenantId { get; set; }
        public required string OrderId { get; set; }
        public required string DevicePatientId { get; set; }
        public required string TestType { get; set; }
        public required DateTime TestDate { get; set; }
        public string? QualityGrade { get; set; }
        public required string SourceFileName { get; set; }
        public required string ContentHash { get; set; }
        public string[] Warnings { get; set; } = [];
        public DateTime ImportedAtUtc { get; set; } = DateTime.UtcNow;
    }
}

