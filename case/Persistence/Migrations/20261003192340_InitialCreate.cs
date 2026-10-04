using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spirometry.Importer.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "import_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    order_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    warnings = table.Column<string[]>(type: "text[]", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_import_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenant",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    employee_ref = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    order_date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.id);
                    table.UniqueConstraint("AK_orders_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_orders_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "spirometry_tests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    order_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    device_patient_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    test_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    test_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    quality_grade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    source_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    warnings = table.Column<string[]>(type: "text[]", nullable: false),
                    imported_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_spirometry_tests", x => x.id);
                    table.UniqueConstraint("AK_spirometry_tests_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_spirometry_tests_orders_tenant_id_order_id",
                        columns: x => new { x.tenant_id, x.order_id },
                        principalTable: "orders",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "spirometry_measurements",
                columns: table => new
                {
                    test_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    value = table.Column<decimal>(type: "numeric", nullable: false),
                    unit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    predicted_value = table.Column<decimal>(type: "numeric", nullable: true),
                    lower_limit_of_normal = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_spirometry_measurements", x => new { x.test_id, x.code });
                    table.ForeignKey(
                        name: "FK_spirometry_measurements_spirometry_tests_tenant_id_test_id",
                        columns: x => new { x.tenant_id, x.test_id },
                        principalTable: "spirometry_tests",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_import_logs_run_id",
                table: "import_logs",
                column: "run_id");

            migrationBuilder.CreateIndex(
                name: "IX_import_logs_tenant_id_created_at_utc",
                table: "import_logs",
                columns: new[] { "tenant_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_spirometry_measurements_tenant_id_test_id",
                table: "spirometry_measurements",
                columns: new[] { "tenant_id", "test_id" });

            migrationBuilder.CreateIndex(
                name: "ux_spirometry_tests_identity",
                table: "spirometry_tests",
                columns: new[] { "tenant_id", "order_id", "device_patient_id", "test_date", "test_type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "import_logs");

            migrationBuilder.DropTable(
                name: "spirometry_measurements");

            migrationBuilder.DropTable(
                name: "spirometry_tests");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "tenant");
        }
    }
}
