using Spirometry.Importer.Models;
using Spirometry.Importer.Persistence.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Spirometry.Importer.Persistence
{
    public sealed class OrderStore
    {
        //iş emirlerini verilen contet üzerinden async kayıt edicez
        public async Task<int> SaveAsync(SpirometryDbContext db, IReadOnlyCollection<Order> orders)
        {


            foreach (Order order in orders)
            {
                TenantEntity? tenant = await db.Tenants.FindAsync(order.TenantId);


                if (tenant is null)
                {
                    tenant = new TenantEntity
                    {
                        Id = order.TenantId,
                        Name = order.TenantName,

                    };
                    db.Tenants.Add(tenant);
                }
                else if (tenant.Name != order.TenantName)
                {
                    throw new InvalidDataException("TENANT_CONFLICT: Ayni kiraci kimligi farkli adla geldi.");
                }
                OrderEntity? existingOrder = await db.Orders.FindAsync(order.OrderId);

                if (existingOrder is not null)
                {
                    bool hasConflict =
                       existingOrder.TenantId != order.TenantId ||
                       existingOrder.EmployeeRef != order.EmployeeRef ||
                       existingOrder.OrderDate != order.OrderDate;

                    if (hasConflict)
                    {
                        throw new InvalidDataException("ORDER_CONFLICT: Mevcut is emri CSV bilgileriyle uyusmuyor.");

                    }
                    continue;
                }
                var entity = new OrderEntity
                {
                    Id = order.OrderId,
                    TenantId = order.TenantId,
                    EmployeeRef = order.EmployeeRef,
                    OrderDate = order.OrderDate,
                };

                db.Orders.Add(entity);

            }
            return await db.SaveChangesAsync();
        }
    }
}
