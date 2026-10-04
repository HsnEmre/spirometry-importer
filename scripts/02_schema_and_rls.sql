CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003192340_InitialCreate') THEN
    CREATE TABLE import_logs (
        id uuid NOT NULL,
        run_id uuid NOT NULL,
        file_name character varying(255) NOT NULL,
        tenant_id character varying(50),
        order_id character varying(50),
        status character varying(30) NOT NULL,
        code character varying(100) NOT NULL,
        warnings text[] NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT "PK_import_logs" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003192340_InitialCreate') THEN
    CREATE TABLE tenant (
        "Id" character varying(50) NOT NULL,
        name character varying(200) NOT NULL,
        CONSTRAINT "PK_tenant" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003192340_InitialCreate') THEN
    CREATE TABLE orders (
        id character varying(50) NOT NULL,
        tenant_id character varying(50) NOT NULL,
        employee_ref character varying(100) NOT NULL,
        order_date date NOT NULL,
        CONSTRAINT "PK_orders" PRIMARY KEY (id),
        CONSTRAINT "AK_orders_tenant_id_id" UNIQUE (tenant_id, id),
        CONSTRAINT "FK_orders_tenant_tenant_id" FOREIGN KEY (tenant_id) REFERENCES tenant ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003192340_InitialCreate') THEN
    CREATE TABLE spirometry_tests (
        id uuid NOT NULL,
        tenant_id character varying(50) NOT NULL,
        order_id character varying(50) NOT NULL,
        device_patient_id character varying(100) NOT NULL,
        test_type character varying(30) NOT NULL,
        test_date timestamp without time zone NOT NULL,
        quality_grade character varying(20),
        source_file_name character varying(255) NOT NULL,
        content_hash character varying(64) NOT NULL,
        warnings text[] NOT NULL,
        imported_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT "PK_spirometry_tests" PRIMARY KEY (id),
        CONSTRAINT "AK_spirometry_tests_tenant_id_id" UNIQUE (tenant_id, id),
        CONSTRAINT "FK_spirometry_tests_orders_tenant_id_order_id" FOREIGN KEY (tenant_id, order_id) REFERENCES orders (tenant_id, id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003192340_InitialCreate') THEN
    CREATE TABLE spirometry_measurements (
        test_id uuid NOT NULL,
        code character varying(50) NOT NULL,
        tenant_id character varying(50) NOT NULL,
        value numeric NOT NULL,
        unit character varying(30) NOT NULL,
        predicted_value numeric,
        lower_limit_of_normal numeric,
        CONSTRAINT "PK_spirometry_measurements" PRIMARY KEY (test_id, code),
        CONSTRAINT "FK_spirometry_measurements_spirometry_tests_tenant_id_test_id" FOREIGN KEY (tenant_id, test_id) REFERENCES spirometry_tests (tenant_id, id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003192340_InitialCreate') THEN
    CREATE INDEX "IX_import_logs_run_id" ON import_logs (run_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003192340_InitialCreate') THEN
    CREATE INDEX "IX_import_logs_tenant_id_created_at_utc" ON import_logs (tenant_id, created_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003192340_InitialCreate') THEN
    CREATE INDEX "IX_spirometry_measurements_tenant_id_test_id" ON spirometry_measurements (tenant_id, test_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003192340_InitialCreate') THEN
    CREATE UNIQUE INDEX ux_spirometry_tests_identity ON spirometry_tests (tenant_id, order_id, device_patient_id, test_date, test_type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003192340_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261003192340_InitialCreate', '10.0.12');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003194930_AddRowLevelSecurity') THEN
    GRANT CONNECT ON DATABASE spirometry_db
    TO spirometry_importer, spirometry_osgb_a, spirometry_osgb_b;

    GRANT USAGE ON SCHEMA public
    TO spirometry_importer, spirometry_osgb_a, spirometry_osgb_b;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003194930_AddRowLevelSecurity') THEN
    -- Import hesabına yalnızca okuma ve ekleme yetkisi verir.
    GRANT SELECT, INSERT ON TABLE public.tenant
    TO spirometry_importer;

    -- OSGB hesaplarına yalnızca okuma yetkisi verir.
    GRANT SELECT ON TABLE public.tenant
    TO spirometry_osgb_a, spirometry_osgb_b;

    -- Bu tabloda satır seviyesinde güvenliği etkinleştirir.
    ALTER TABLE public.tenant
    ENABLE ROW LEVEL SECURITY;

    -- Normal tablo sahibini de RLS kurallarına tabi tutar.
    ALTER TABLE public.tenant
    FORCE ROW LEVEL SECURITY;

    -- Güvenilir import hesabı bütün kiracıların mevcut kayıtlarını okuyabilir.
    CREATE POLICY importer_select
    ON public.tenant
    FOR SELECT
    TO spirometry_importer
    USING (true);

    -- Güvenilir import hesabı bütün kiracılar için kayıt ekleyebilir.
    CREATE POLICY importer_insert
    ON public.tenant
    FOR INSERT
    TO spirometry_importer
    WITH CHECK (true);

    -- OSGB A yalnızca T-01 kiracısının satırlarını okuyabilir.
    CREATE POLICY osgb_a_select
    ON public.tenant
    FOR SELECT
    TO spirometry_osgb_a
    USING ("Id" = 'T-01');

    -- OSGB B yalnızca T-02 kiracısının satırlarını okuyabilir.
    CREATE POLICY osgb_b_select
    ON public.tenant
    FOR SELECT
    TO spirometry_osgb_b
    USING ("Id" = 'T-02');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003194930_AddRowLevelSecurity') THEN
    -- Import hesabına yalnızca okuma ve ekleme yetkisi verir.
    GRANT SELECT, INSERT ON TABLE public.orders
    TO spirometry_importer;

    -- OSGB hesaplarına yalnızca okuma yetkisi verir.
    GRANT SELECT ON TABLE public.orders
    TO spirometry_osgb_a, spirometry_osgb_b;

    -- Bu tabloda satır seviyesinde güvenliği etkinleştirir.
    ALTER TABLE public.orders
    ENABLE ROW LEVEL SECURITY;

    -- Normal tablo sahibini de RLS kurallarına tabi tutar.
    ALTER TABLE public.orders
    FORCE ROW LEVEL SECURITY;

    -- Güvenilir import hesabı bütün kiracıların mevcut kayıtlarını okuyabilir.
    CREATE POLICY importer_select
    ON public.orders
    FOR SELECT
    TO spirometry_importer
    USING (true);

    -- Güvenilir import hesabı bütün kiracılar için kayıt ekleyebilir.
    CREATE POLICY importer_insert
    ON public.orders
    FOR INSERT
    TO spirometry_importer
    WITH CHECK (true);

    -- OSGB A yalnızca T-01 kiracısının satırlarını okuyabilir.
    CREATE POLICY osgb_a_select
    ON public.orders
    FOR SELECT
    TO spirometry_osgb_a
    USING (tenant_id = 'T-01');

    -- OSGB B yalnızca T-02 kiracısının satırlarını okuyabilir.
    CREATE POLICY osgb_b_select
    ON public.orders
    FOR SELECT
    TO spirometry_osgb_b
    USING (tenant_id = 'T-02');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003194930_AddRowLevelSecurity') THEN
    -- Import hesabına yalnızca okuma ve ekleme yetkisi verir.
    GRANT SELECT, INSERT ON TABLE public.spirometry_tests
    TO spirometry_importer;

    -- OSGB hesaplarına yalnızca okuma yetkisi verir.
    GRANT SELECT ON TABLE public.spirometry_tests
    TO spirometry_osgb_a, spirometry_osgb_b;

    -- Bu tabloda satır seviyesinde güvenliği etkinleştirir.
    ALTER TABLE public.spirometry_tests
    ENABLE ROW LEVEL SECURITY;

    -- Normal tablo sahibini de RLS kurallarına tabi tutar.
    ALTER TABLE public.spirometry_tests
    FORCE ROW LEVEL SECURITY;

    -- Güvenilir import hesabı bütün kiracıların mevcut kayıtlarını okuyabilir.
    CREATE POLICY importer_select
    ON public.spirometry_tests
    FOR SELECT
    TO spirometry_importer
    USING (true);

    -- Güvenilir import hesabı bütün kiracılar için kayıt ekleyebilir.
    CREATE POLICY importer_insert
    ON public.spirometry_tests
    FOR INSERT
    TO spirometry_importer
    WITH CHECK (true);

    -- OSGB A yalnızca T-01 kiracısının satırlarını okuyabilir.
    CREATE POLICY osgb_a_select
    ON public.spirometry_tests
    FOR SELECT
    TO spirometry_osgb_a
    USING (tenant_id = 'T-01');

    -- OSGB B yalnızca T-02 kiracısının satırlarını okuyabilir.
    CREATE POLICY osgb_b_select
    ON public.spirometry_tests
    FOR SELECT
    TO spirometry_osgb_b
    USING (tenant_id = 'T-02');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003194930_AddRowLevelSecurity') THEN
    -- Import hesabına yalnızca okuma ve ekleme yetkisi verir.
    GRANT SELECT, INSERT ON TABLE public.spirometry_measurements
    TO spirometry_importer;

    -- OSGB hesaplarına yalnızca okuma yetkisi verir.
    GRANT SELECT ON TABLE public.spirometry_measurements
    TO spirometry_osgb_a, spirometry_osgb_b;

    -- Bu tabloda satır seviyesinde güvenliği etkinleştirir.
    ALTER TABLE public.spirometry_measurements
    ENABLE ROW LEVEL SECURITY;

    -- Normal tablo sahibini de RLS kurallarına tabi tutar.
    ALTER TABLE public.spirometry_measurements
    FORCE ROW LEVEL SECURITY;

    -- Güvenilir import hesabı bütün kiracıların mevcut kayıtlarını okuyabilir.
    CREATE POLICY importer_select
    ON public.spirometry_measurements
    FOR SELECT
    TO spirometry_importer
    USING (true);

    -- Güvenilir import hesabı bütün kiracılar için kayıt ekleyebilir.
    CREATE POLICY importer_insert
    ON public.spirometry_measurements
    FOR INSERT
    TO spirometry_importer
    WITH CHECK (true);

    -- OSGB A yalnızca T-01 kiracısının satırlarını okuyabilir.
    CREATE POLICY osgb_a_select
    ON public.spirometry_measurements
    FOR SELECT
    TO spirometry_osgb_a
    USING (tenant_id = 'T-01');

    -- OSGB B yalnızca T-02 kiracısının satırlarını okuyabilir.
    CREATE POLICY osgb_b_select
    ON public.spirometry_measurements
    FOR SELECT
    TO spirometry_osgb_b
    USING (tenant_id = 'T-02');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003194930_AddRowLevelSecurity') THEN
    -- Import hesabına yalnızca okuma ve ekleme yetkisi verir.
    GRANT SELECT, INSERT ON TABLE public.import_logs
    TO spirometry_importer;

    -- OSGB hesaplarına yalnızca okuma yetkisi verir.
    GRANT SELECT ON TABLE public.import_logs
    TO spirometry_osgb_a, spirometry_osgb_b;

    -- Bu tabloda satır seviyesinde güvenliği etkinleştirir.
    ALTER TABLE public.import_logs
    ENABLE ROW LEVEL SECURITY;

    -- Normal tablo sahibini de RLS kurallarına tabi tutar.
    ALTER TABLE public.import_logs
    FORCE ROW LEVEL SECURITY;

    -- Güvenilir import hesabı bütün kiracıların mevcut kayıtlarını okuyabilir.
    CREATE POLICY importer_select
    ON public.import_logs
    FOR SELECT
    TO spirometry_importer
    USING (true);

    -- Güvenilir import hesabı bütün kiracılar için kayıt ekleyebilir.
    CREATE POLICY importer_insert
    ON public.import_logs
    FOR INSERT
    TO spirometry_importer
    WITH CHECK (true);

    -- OSGB A yalnızca T-01 kiracısının satırlarını okuyabilir.
    CREATE POLICY osgb_a_select
    ON public.import_logs
    FOR SELECT
    TO spirometry_osgb_a
    USING (tenant_id = 'T-01');

    -- OSGB B yalnızca T-02 kiracısının satırlarını okuyabilir.
    CREATE POLICY osgb_b_select
    ON public.import_logs
    FOR SELECT
    TO spirometry_osgb_b
    USING (tenant_id = 'T-02');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003194930_AddRowLevelSecurity') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261003194930_AddRowLevelSecurity', '10.0.12');
    END IF;
END $EF$;
COMMIT;

