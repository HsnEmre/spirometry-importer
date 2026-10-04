using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spirometry.Importer.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRowLevelSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Hesaplara bağlantı ve şema kullanım izni verir.
            migrationBuilder.Sql("""
        GRANT CONNECT ON DATABASE spirometry_db
        TO spirometry_importer, spirometry_osgb_a, spirometry_osgb_b;

        GRANT USAGE ON SCHEMA public
        TO spirometry_importer, spirometry_osgb_a, spirometry_osgb_b;
        """);

            // Korunacak tabloları ve her tablonun kiracı kimliğini tutan sütununu tanımlar.
            var tables = new[]
            {
        // Veritabanındaki büyük I ile oluşturulmuş Id sütununu çift tırnakla kullanır.
        (TableName: "tenant", TenantColumn: "\"Id\""),

        // İş emrinin kiracısı tenant_id sütunundadır.
        (TableName: "orders", TenantColumn: "tenant_id"),

        // Testin kiracısı tenant_id sütunundadır.
        (TableName: "spirometry_tests", TenantColumn: "tenant_id"),

        // Ölçümün kiracısı tenant_id sütunundadır.
        (TableName: "spirometry_measurements", TenantColumn: "tenant_id"),

        // Logun kiracısı belirlenebildiyse tenant_id sütunundadır.
        (TableName: "import_logs", TenantColumn: "tenant_id")
    };

            // Aynı güvenlik düzenini beş tabloya da uygular.
            foreach (var table in tables)
            {
                // Tabloya ait izinleri ve politikaları PostgreSQL'de oluşturur.
                migrationBuilder.Sql($"""
            -- Import hesabına yalnızca okuma ve ekleme yetkisi verir.
            GRANT SELECT, INSERT ON TABLE public.{table.TableName}
            TO spirometry_importer;

            -- OSGB hesaplarına yalnızca okuma yetkisi verir.
            GRANT SELECT ON TABLE public.{table.TableName}
            TO spirometry_osgb_a, spirometry_osgb_b;

            -- Bu tabloda satır seviyesinde güvenliği etkinleştirir.
            ALTER TABLE public.{table.TableName}
            ENABLE ROW LEVEL SECURITY;

            -- Normal tablo sahibini de RLS kurallarına tabi tutar.
            ALTER TABLE public.{table.TableName}
            FORCE ROW LEVEL SECURITY;

            -- Güvenilir import hesabı bütün kiracıların mevcut kayıtlarını okuyabilir.
            CREATE POLICY importer_select
            ON public.{table.TableName}
            FOR SELECT
            TO spirometry_importer
            USING (true);

            -- Güvenilir import hesabı bütün kiracılar için kayıt ekleyebilir.
            CREATE POLICY importer_insert
            ON public.{table.TableName}
            FOR INSERT
            TO spirometry_importer
            WITH CHECK (true);

            -- OSGB A yalnızca T-01 kiracısının satırlarını okuyabilir.
            CREATE POLICY osgb_a_select
            ON public.{table.TableName}
            FOR SELECT
            TO spirometry_osgb_a
            USING ({table.TenantColumn} = 'T-01');

            -- OSGB B yalnızca T-02 kiracısının satırlarını okuyabilir.
            CREATE POLICY osgb_b_select
            ON public.{table.TableName}
            FOR SELECT
            TO spirometry_osgb_b
            USING ({table.TenantColumn} = 'T-02');
            """);
            }

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Bu migration'ın güvenlik kurallarını uyguladığı tabloları tanımlar.
            string[] tables =
            [
                "import_logs",
        "spirometry_measurements",
        "spirometry_tests",
        "orders",
        "tenants"
            ];
            // Her tablodaki güvenlik değişikliklerini geri alır.
            foreach (string table in tables)
            {
                // Önce hesapların tablo erişimini kaldırır, ardından politikaları temizler.
                migrationBuilder.Sql($"""
            -- Import hesabının bu migration ile verilen tablo izinlerini kaldırır.
            REVOKE SELECT, INSERT ON TABLE public.{table}
            FROM spirometry_importer;

            -- OSGB hesaplarının okuma izinlerini kaldırır.
            REVOKE SELECT ON TABLE public.{table}
            FROM spirometry_osgb_a, spirometry_osgb_b;

            -- Import hesabının okuma politikasını kaldırır.
            DROP POLICY IF EXISTS importer_select ON public.{table};

            -- Import hesabının ekleme politikasını kaldırır.
            DROP POLICY IF EXISTS importer_insert ON public.{table};

            -- OSGB A'nın okuma politikasını kaldırır.
            DROP POLICY IF EXISTS osgb_a_select ON public.{table};

            -- OSGB B'nin okuma politikasını kaldırır.
            DROP POLICY IF EXISTS osgb_b_select ON public.{table};

            -- Tablo sahibine RLS uygulama zorunluluğunu kaldırır.
            ALTER TABLE public.{table} NO FORCE ROW LEVEL SECURITY;

            -- Bu migration ile etkinleştirilen RLS'yi kapatır.
            ALTER TABLE public.{table} DISABLE ROW LEVEL SECURITY;
            """);
            }
        }
    }
}
