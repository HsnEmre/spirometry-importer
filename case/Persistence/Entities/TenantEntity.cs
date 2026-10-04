using System;
using System.Collections.Generic;
using System.Text;

namespace Spirometry.Importer.Persistence.Entities
{
    public sealed class TenantEntity
    {
        public required string Id { get; set; }//T-01
        public required string Name { get; set; }    //osgb name
    }
}
