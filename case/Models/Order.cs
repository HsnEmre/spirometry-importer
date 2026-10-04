using System;
using System.Collections.Generic;
using System.Text;

namespace Spirometry.Importer.Models
{
    //model sinifi turetilmesi engellemek icin muhurlu
    //tek is emri satiri
    public sealed class Order
    {
        //is emri id order-1001
        public required string OrderId { get; init; }

        //bagli osgb kimligi Tenant-1001
        public required string TenantId { get; init; }

        //kiraci adi 
        public required string TenantName { get; init; }
        //is emri bagli referans
        public required string EmployeeRef { get; init; }


        public required DateOnly OrderDate { get; init; }

        //required olmasi deger atamasinin zorunlu kilar  nesne olusurken
        // init olmasi sadece nesne olusturuken deger atabilirsin sonrasinda atamazsin

    }
}
