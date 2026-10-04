using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;
using Spirometry.Importer.Persistence.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Spirometry.Importer.Persistence
{
    public sealed class SpirometryDbContextFactory
         : IDesignTimeDbContextFactory<SpirometryDbContext>
    {
        public SpirometryDbContext CreateDbContext(string[] args)
        {
            
            string? password = Environment.GetEnvironmentVariable(
                "SPIROMETRY_MIGRATION_PASSWORD");

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    "SPIROMETRY_MIGRATION_PASSWORD ortam degiskenini ayarlayin.");
            }

            var connectionBuilder = new NpgsqlConnectionStringBuilder
            {
                Host = "localhost",
                Port = 5432,
                Database = "spirometry_db",
                Username = "postgres",
                Password = password
            };

            
            var optionsBuilder =
                new DbContextOptionsBuilder<SpirometryDbContext>();


            //optionsBuilder.UseNpgsql(connectionBuilder.ConnectionString); sadece konsolda çalışan versiyon

            // PostgreSQL bağlantısını ve migration'ların bulunduğu assembly'yi belirtir.
            optionsBuilder.UseNpgsql(
                connectionBuilder.ConnectionString,
                options => options.MigrationsAssembly("Spirometry.Importer"));


            return new SpirometryDbContext(optionsBuilder.Options);
        }
    }
}
