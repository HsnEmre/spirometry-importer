using System;
using System.Collections.Generic;
using System.Text;

namespace Spirometry.Importer.Models
{
    public sealed class SpirometryTest//test modelinin bulundugu tablo
    {

        public required string OrderId { get; init; }//testin  bagli oldugu is emri
        public required string DevicePatientId { get; init; } //xml de verilen hasta referansi syn-0001
        public required string TestType { get; init; }//test turu fvc
        public required DateTime TestDate { get; init; }
        public string? QualityGrade { get; init; }// kalite derecesi

        public List<SpirometryMeasurement> Measurements { get; init; } = new();
        public List<string> Warnings { get; init; } = new();


        //tanantid yoksa kiraciyi xmlden degil orderid uzerinden csvden alnacak
        //cihazdaki hasta referansi ile calisan referansi ayri
        
    }
}
