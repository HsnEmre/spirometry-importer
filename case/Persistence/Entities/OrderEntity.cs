using System;
using System.Collections.Generic;
using System.Text;

namespace Spirometry.Importer.Persistence.Entities
{
    public sealed class OrderEntity
    {
        public required string Id { get; set; }//0-1001
        public required string TenantId { get; set; }//osgb kimlik
        public required string EmployeeRef { get; set; }// iş emrine bağlı çalışan kimlik
        public required DateOnly OrderDate { get; set; }

    }
}
