using System;
using System.Collections.Generic;
using System.Text;

namespace Spirometry.Importer.Persistence.Entities
{
    public sealed class ImportLogEntity
    {
        // Her log kaydı için benzersiz kimlik oluşturur.
        public Guid Id { get; set; } = Guid.NewGuid();

        // Aynı çalıştırmada işlenen dosyaları birlikte takip etmeyi sağlar.
        public required Guid RunId { get; set; }
        public required string FileName { get; set; }
        public string? TenantId { get; set; }
        public string? OrderId { get; set; }
        public required string Status { get; set; }
        public required string Code { get; set; }
        public string[] Warnings { get; set; } = [];

        
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
