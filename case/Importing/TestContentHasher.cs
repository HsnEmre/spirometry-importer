using Spirometry.Importer.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Spirometry.Importer.Importing
{
    public static class TestContentHasher
    {
        public static string Compute(SpirometryTest test)
        {
            var content = new
            {
                OrderId = test.OrderId,
                DevicePatientId = test.DevicePatientId,
                TestType = test.TestType,
                TestDate = test.TestDate.ToString("O", CultureInfo.InvariantCulture),
                QualityGrade = test.QualityGrade,
                Measurements = test.Measurements
                .OrderBy(x => x.Code, StringComparer.Ordinal)
                .Select(x => new
                {
                    Code = x.Code,
                    Value = x.Value.ToString("G29", CultureInfo.InvariantCulture),
                    Unit = x.Unit,
                    PredictedValue = x.PredictedValue?.ToString("G29", CultureInfo.InvariantCulture),
                    LowerLimitOfNormal = x.LowerLimitOfNormal?.ToString("G29", CultureInfo.InvariantCulture)
                }).ToArray()
            };

            byte[] contentBytes = JsonSerializer.SerializeToUtf8Bytes(content);
            byte[] hashBytes = SHA256.HashData(contentBytes);

            return Convert.ToHexString(hashBytes);
        }
    }
}
