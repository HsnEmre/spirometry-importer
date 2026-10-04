using System;
using System.Collections.Generic;
using System.Text;

namespace Spirometry.Importer.Persistence.Entities
{
    public sealed class SpirometryMeasurementEntity
    {
        // Ölçümün ait olduğu OSGB'nin kimliğini tutar.
        public required string TenantId { get; set; }

        // Ölçümün bağlı olduğu test kaydının kimliğini tutar.
        public required Guid TestId { get; set; }

        // Ölçüm kodunu tutar. Örnek: FVC veya FEV1.
        public required string Code { get; set; }
        public required decimal Value { get; set; }
        public required string Unit { get; set; }
        public decimal? PredictedValue { get; set; }
        public decimal? LowerLimitOfNormal { get; set; }
    }
}
