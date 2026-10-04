using Microsoft.EntityFrameworkCore;
using Spirometry.Importer.Importing;
using Spirometry.Importer.Models;
using Spirometry.Importer.Persistence.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Spirometry.Importer.Persistence
{
    /// <summary>
    /// test ölçüm ve işlem loglarının kaydını yönetir
    /// </summary>
    public sealed class SpirometryTestStore
    {
        public async Task<ImportLogEntity> SaveAsync(
            SpirometryDbContext db,
            SpirometryTest test,
            Order order,
            string fileName,
            Guid runId)
        {
            if (test.OrderId != order.OrderId)
            {
                throw new InvalidDataException(
                   "ORDER_MISMATCH: Test ve is emri eslesmiyor.");
            }

            var storedOrder = await db.Orders.FindAsync(order.OrderId);

            if (storedOrder is null)
            {
                throw new InvalidDataException(
                   "ORDER_NOT_FOUND: Is emri veritabaninda bulunamadi.");
            }

            if (storedOrder.TenantId != order.TenantId ||
                storedOrder.EmployeeRef != order.EmployeeRef ||
                storedOrder.OrderDate != order.OrderDate)
            {
                throw new InvalidDataException(
                   "ORDER_CONFLICT: Is emri bilgileri veritabaniyla uyusmuyor.");

            }
            string tenantId = storedOrder.TenantId;
            string contentHash = TestContentHasher.Compute(test);


            var existingTest = await db.SpirometryTests
                .AsNoTracking()
                .SingleOrDefaultAsync(x =>
                    x.TenantId == tenantId &&
                    x.OrderId == test.OrderId &&
                    x.DevicePatientId == test.DevicePatientId &&
                    x.TestDate == test.TestDate &&
                    x.TestType == test.TestType);

            var log = new ImportLogEntity
            {
                RunId = runId,
                FileName = Path.GetFileName(fileName),
                TenantId = tenantId,
                OrderId = storedOrder.Id,
                Status = test.Warnings.Count > 0
                 ? "ImportedWithWarnings"
                    : "Imported",
                Code = test.Warnings.Count > 0
                    ? "MISSING_MEASUREMENTS"
                    : "IMPORT_OK",
                Warnings = test.Warnings.ToArray()
            };

            if (existingTest is not null)
            {
                bool sameContent = existingTest.ContentHash == contentHash;

                log.Status = sameContent ? "Skipped" : "Failed";

                log.Code = sameContent
                    ? "DUPLICATE_TEST"
                    : "TEST_CONTENT_CONFLICT";

                db.ImportLogs.Add(log);
                await db.SaveChangesAsync();

                return log;
            }
            var entity = new SpirometryTestEntity
            {
                TenantId = tenantId,
                OrderId = storedOrder.Id,
                DevicePatientId = test.DevicePatientId,
                TestType = test.TestType,
                TestDate = test.TestDate,
                QualityGrade = test.QualityGrade,
                SourceFileName = Path.GetFileName(fileName),
                ContentHash = contentHash,
                Warnings = test.Warnings.ToArray()
            };

            db.SpirometryTests.Add(entity);

            foreach (var measurement in test.Measurements)
            {
                var measurementEntity = new SpirometryMeasurementEntity
                {
                    TenantId = tenantId,
                    TestId = entity.Id,
                    Code = measurement.Code,
                    Value = measurement.Value,
                    Unit = measurement.Unit,
                    PredictedValue = measurement.PredictedValue,
                    LowerLimitOfNormal = measurement.LowerLimitOfNormal
                };
                db.SpirometryMeasurements.Add(measurementEntity);
            }
            db.ImportLogs.Add(log);

            await db.SaveChangesAsync();

            return log;
        }


    }
}
