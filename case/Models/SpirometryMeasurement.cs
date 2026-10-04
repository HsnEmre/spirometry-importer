using System;
using System.Collections.Generic;
using System.Text;

namespace Spirometry.Importer.Models
{
     public sealed class SpirometryMeasurement// testteki tekk olcumu temsil edecek
    {

        public required string Code { get; init; }//olcum kodu

        public required decimal Value { get; init; }//cihazin olctugu sayisal deger

        public required string  Unit { get; init; }//birim

        public decimal? PredictedValue { get; init; }//beklenen deger yoksa null

        public decimal? LowerLimitOfNormal { get; init; }//alt normal sinir degerini tutar
    }
}
