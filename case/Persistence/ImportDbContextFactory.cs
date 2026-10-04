using Microsoft.EntityFrameworkCore;
using Npgsql;
using Spirometry.Importer.Persistence.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Spirometry.Importer.Persistence
{
    public sealed class ImportDbContextFactory
    {
        // Verilen parola ile yeni bir context oluşturur.
        public SpirometryDbContext Create(string password)
        {
         
            if (string.IsNullOrWhiteSpace(password))
            {
              
                throw new InvalidOperationException(
                    "Import hesabi parolasi gerekli.");
            }

            
            var connectionBuilder = new NpgsqlConnectionStringBuilder
            {
               
                Host = "localhost",

              
                Port = 5432,

              
                Database = "spirometry_db",

             
                Username = "spirometry_importer",

            
                Password = password
            };

           
            var optionsBuilder =
                new DbContextOptionsBuilder<SpirometryDbContext>();


            //optionsBuilder.UseNpgsql(connectionBuilder.ConnectionString); sadece konsolda çalışan versiyon

            // PostgreSQL bağlantısını ve migration'ların bulunduğu assembly'yi belirtir.
            optionsBuilder.UseNpgsql(
                connectionBuilder.ConnectionString,
                options => options.MigrationsAssembly("Spirometry.Importer"));
            // Her çağrıda yeni bir context döndürür.
            return new SpirometryDbContext(optionsBuilder.Options);
        }
    }
}
