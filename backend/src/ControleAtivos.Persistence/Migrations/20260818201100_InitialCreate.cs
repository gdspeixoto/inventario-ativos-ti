using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControleAtivos.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "controle_ativos");

            migrationBuilder.CreateTable(
                name: "alerts",
                schema: "controle_ativos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizational_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    recommended_action = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    assigned_to_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resolved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    resolution_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    deduplication_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_alerts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                schema: "controle_ativos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    metadata_json = table.Column<string>(type: "jsonb", maxLength: 200, nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "documents",
                schema: "controle_ativos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    size_in_bytes = table.Column<long>(type: "bigint", nullable: false),
                    storage_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    uploaded_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_documents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "organizational_units",
                schema: "controle_ativos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organizational_units", x => x.id);
                    table.ForeignKey(
                        name: "fk_organizational_units_organizational_units_parent_id",
                        column: x => x.parent_id,
                        principalSchema: "controle_ativos",
                        principalTable: "organizational_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "price_adjustments",
                schema: "controle_ativos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizational_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    effective_date = table.Column<DateOnly>(type: "date", nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    index_applied = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    document_id = table.Column<Guid>(type: "uuid", nullable: true),
                    new_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    new_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    previous_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    previous_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_price_adjustments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "suppliers",
                schema: "controle_ativos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    document_number = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    main_contact_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    website = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    sla_description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_suppliers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenants",
                schema: "controle_ativos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "timeline_events",
                schema: "controle_ativos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    organizational_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    previous_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    new_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_timeline_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cost_centers",
                schema: "controle_ativos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    organizational_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cost_centers", x => x.id);
                    table.ForeignKey(
                        name: "fk_cost_centers_organizational_units_organizational_unit_id",
                        column: x => x.organizational_unit_id,
                        principalSchema: "controle_ativos",
                        principalTable: "organizational_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "controle_ativos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    job_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    primary_organizational_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_organizational_units_primary_organizational_unit_id",
                        column: x => x.primary_organizational_unit_id,
                        principalSchema: "controle_ativos",
                        principalTable: "organizational_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "contracts",
                schema: "controle_ativos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizational_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    adjustment_index = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    adjustment_periodicity = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    last_adjustment_date = table.Column<DateOnly>(type: "date", nullable: true),
                    next_adjustment_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    internal_responsible_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    monthly_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contracts", x => x.id);
                    table.ForeignKey(
                        name: "fk_contracts_organizational_units_organizational_unit_id",
                        column: x => x.organizational_unit_id,
                        principalSchema: "controle_ativos",
                        principalTable: "organizational_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_contracts_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalSchema: "controle_ativos",
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_contracts_users_internal_responsible_user_id",
                        column: x => x.internal_responsible_user_id,
                        principalSchema: "controle_ativos",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "management_assignments",
                schema: "controle_ativos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizational_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    includes_descendants = table.Column<bool>(type: "boolean", nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_management_assignments", x => x.id);
                    table.ForeignKey(
                        name: "fk_management_assignments_organizational_units_organizational_~",
                        column: x => x.organizational_unit_id,
                        principalSchema: "controle_ativos",
                        principalTable: "organizational_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_management_assignments_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "controle_ativos",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "assets",
                schema: "controle_ativos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    billing_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    organizational_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cost_center_id = table.Column<Guid>(type: "uuid", nullable: true),
                    technical_responsible_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    financial_responsible_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    renewal_date = table.Column<DateOnly>(type: "date", nullable: true),
                    last_adjustment_date = table.Column<DateOnly>(type: "date", nullable: true),
                    next_adjustment_date = table.Column<DateOnly>(type: "date", nullable: true),
                    monthly_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    manufacturer = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    product = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    plan = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    contracted_quantity = table.Column<int>(type: "integer", nullable: true),
                    used_quantity = table.Column<int>(type: "integer", nullable: true),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    unit_price_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    hostname = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: true),
                    server_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    environment = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    operating_system = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    operating_system_version = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    provider = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    region_or_datacenter = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    primary_ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    cpu_cores = table.Column<int>(type: "integer", nullable: true),
                    memory_gb = table.Column<int>(type: "integer", nullable: true),
                    storage_gb = table.Column<int>(type: "integer", nullable: true),
                    backup_policy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    last_backup_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    sla = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    maintenance_window = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    backup_monthly_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    backup_cost_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    infrastructure_monthly_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    infrastructure_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    license_monthly_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    license_cost_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    support_monthly_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    support_cost_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assets", x => x.id);
                    table.ForeignKey(
                        name: "fk_assets_contracts_contract_id",
                        column: x => x.contract_id,
                        principalSchema: "controle_ativos",
                        principalTable: "contracts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_assets_cost_centers_cost_center_id",
                        column: x => x.cost_center_id,
                        principalSchema: "controle_ativos",
                        principalTable: "cost_centers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_assets_organizational_units_organizational_unit_id",
                        column: x => x.organizational_unit_id,
                        principalSchema: "controle_ativos",
                        principalTable: "organizational_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_assets_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalSchema: "controle_ativos",
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_assets_users_financial_responsible_user_id",
                        column: x => x.financial_responsible_user_id,
                        principalSchema: "controle_ativos",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_assets_users_technical_responsible_user_id",
                        column: x => x.technical_responsible_user_id,
                        principalSchema: "controle_ativos",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "cost_entries",
                schema: "controle_ativos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizational_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: true),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cost_center_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    competence_month = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_date = table.Column<DateOnly>(type: "date", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cost_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_cost_entries_assets_asset_id",
                        column: x => x.asset_id,
                        principalSchema: "controle_ativos",
                        principalTable: "assets",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_cost_entries_contracts_contract_id",
                        column: x => x.contract_id,
                        principalSchema: "controle_ativos",
                        principalTable: "contracts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_cost_entries_cost_centers_cost_center_id",
                        column: x => x.cost_center_id,
                        principalSchema: "controle_ativos",
                        principalTable: "cost_centers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_cost_entries_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalSchema: "controle_ativos",
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_alerts_open_deduplication",
                schema: "controle_ativos",
                table: "alerts",
                columns: new[] { "tenant_id", "deduplication_key" },
                unique: true,
                filter: "status IN ('Aberto', 'EmAnalise')");

            migrationBuilder.CreateIndex(
                name: "ix_alerts_tenant_id_due_date",
                schema: "controle_ativos",
                table: "alerts",
                columns: new[] { "tenant_id", "due_date" });

            migrationBuilder.CreateIndex(
                name: "ix_alerts_tenant_id_organizational_unit_id_status",
                schema: "controle_ativos",
                table: "alerts",
                columns: new[] { "tenant_id", "organizational_unit_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_alerts_tenant_id_status_priority",
                schema: "controle_ativos",
                table: "alerts",
                columns: new[] { "tenant_id", "status", "priority" });

            migrationBuilder.CreateIndex(
                name: "ix_assets_contract_id",
                schema: "controle_ativos",
                table: "assets",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "ix_assets_cost_center_id",
                schema: "controle_ativos",
                table: "assets",
                column: "cost_center_id");

            migrationBuilder.CreateIndex(
                name: "ix_assets_financial_responsible_user_id",
                schema: "controle_ativos",
                table: "assets",
                column: "financial_responsible_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_assets_organizational_unit_id",
                schema: "controle_ativos",
                table: "assets",
                column: "organizational_unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_assets_supplier_id",
                schema: "controle_ativos",
                table: "assets",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_assets_technical_responsible_user_id",
                schema: "controle_ativos",
                table: "assets",
                column: "technical_responsible_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_assets_tenant_id_code",
                schema: "controle_ativos",
                table: "assets",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_assets_tenant_id_contract_id",
                schema: "controle_ativos",
                table: "assets",
                columns: new[] { "tenant_id", "contract_id" });

            migrationBuilder.CreateIndex(
                name: "ix_assets_tenant_id_kind_status",
                schema: "controle_ativos",
                table: "assets",
                columns: new[] { "tenant_id", "kind", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_assets_tenant_id_organizational_unit_id",
                schema: "controle_ativos",
                table: "assets",
                columns: new[] { "tenant_id", "organizational_unit_id" });

            migrationBuilder.CreateIndex(
                name: "ix_assets_tenant_id_renewal_date",
                schema: "controle_ativos",
                table: "assets",
                columns: new[] { "tenant_id", "renewal_date" });

            migrationBuilder.CreateIndex(
                name: "ix_assets_tenant_id_status",
                schema: "controle_ativos",
                table: "assets",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_correlation_id",
                schema: "controle_ativos",
                table: "audit_logs",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_tenant_id_entity_type_entity_id",
                schema: "controle_ativos",
                table: "audit_logs",
                columns: new[] { "tenant_id", "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_tenant_id_occurred_at",
                schema: "controle_ativos",
                table: "audit_logs",
                columns: new[] { "tenant_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_tenant_id_user_id_occurred_at",
                schema: "controle_ativos",
                table: "audit_logs",
                columns: new[] { "tenant_id", "user_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_contracts_internal_responsible_user_id",
                schema: "controle_ativos",
                table: "contracts",
                column: "internal_responsible_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_contracts_organizational_unit_id",
                schema: "controle_ativos",
                table: "contracts",
                column: "organizational_unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_contracts_supplier_id",
                schema: "controle_ativos",
                table: "contracts",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_contracts_tenant_id_end_date",
                schema: "controle_ativos",
                table: "contracts",
                columns: new[] { "tenant_id", "end_date" });

            migrationBuilder.CreateIndex(
                name: "ix_contracts_tenant_id_number",
                schema: "controle_ativos",
                table: "contracts",
                columns: new[] { "tenant_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_contracts_tenant_id_status",
                schema: "controle_ativos",
                table: "contracts",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_contracts_tenant_id_supplier_id",
                schema: "controle_ativos",
                table: "contracts",
                columns: new[] { "tenant_id", "supplier_id" });

            migrationBuilder.CreateIndex(
                name: "ix_cost_centers_organizational_unit_id",
                schema: "controle_ativos",
                table: "cost_centers",
                column: "organizational_unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_cost_centers_tenant_id_code",
                schema: "controle_ativos",
                table: "cost_centers",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cost_entries_asset_id",
                schema: "controle_ativos",
                table: "cost_entries",
                column: "asset_id");

            migrationBuilder.CreateIndex(
                name: "ix_cost_entries_contract_id",
                schema: "controle_ativos",
                table: "cost_entries",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "ix_cost_entries_cost_center_id",
                schema: "controle_ativos",
                table: "cost_entries",
                column: "cost_center_id");

            migrationBuilder.CreateIndex(
                name: "ix_cost_entries_supplier_id",
                schema: "controle_ativos",
                table: "cost_entries",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_cost_entries_tenant_id_asset_id_competence_month",
                schema: "controle_ativos",
                table: "cost_entries",
                columns: new[] { "tenant_id", "asset_id", "competence_month" });

            migrationBuilder.CreateIndex(
                name: "ix_cost_entries_tenant_id_competence_month",
                schema: "controle_ativos",
                table: "cost_entries",
                columns: new[] { "tenant_id", "competence_month" });

            migrationBuilder.CreateIndex(
                name: "ix_cost_entries_tenant_id_organizational_unit_id_competence_mo~",
                schema: "controle_ativos",
                table: "cost_entries",
                columns: new[] { "tenant_id", "organizational_unit_id", "competence_month" });

            migrationBuilder.CreateIndex(
                name: "ix_documents_tenant_id_entity_type_entity_id",
                schema: "controle_ativos",
                table: "documents",
                columns: new[] { "tenant_id", "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_documents_tenant_id_sha256",
                schema: "controle_ativos",
                table: "documents",
                columns: new[] { "tenant_id", "sha256" });

            migrationBuilder.CreateIndex(
                name: "ix_management_assignments_active_unique",
                schema: "controle_ativos",
                table: "management_assignments",
                columns: new[] { "tenant_id", "user_id", "organizational_unit_id", "role" },
                unique: true,
                filter: "end_date IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_management_assignments_organizational_unit_id",
                schema: "controle_ativos",
                table: "management_assignments",
                column: "organizational_unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_management_assignments_tenant_id_organizational_unit_id",
                schema: "controle_ativos",
                table: "management_assignments",
                columns: new[] { "tenant_id", "organizational_unit_id" });

            migrationBuilder.CreateIndex(
                name: "ix_management_assignments_tenant_id_user_id_end_date",
                schema: "controle_ativos",
                table: "management_assignments",
                columns: new[] { "tenant_id", "user_id", "end_date" });

            migrationBuilder.CreateIndex(
                name: "ix_management_assignments_user_id",
                schema: "controle_ativos",
                table: "management_assignments",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_organizational_units_parent_id",
                schema: "controle_ativos",
                table: "organizational_units",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_organizational_units_tenant_id_code",
                schema: "controle_ativos",
                table: "organizational_units",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_organizational_units_tenant_id_parent_id",
                schema: "controle_ativos",
                table: "organizational_units",
                columns: new[] { "tenant_id", "parent_id" });

            migrationBuilder.CreateIndex(
                name: "ix_organizational_units_tenant_path",
                schema: "controle_ativos",
                table: "organizational_units",
                columns: new[] { "tenant_id", "path" });

            migrationBuilder.CreateIndex(
                name: "ix_price_adjustments_tenant_id_effective_date",
                schema: "controle_ativos",
                table: "price_adjustments",
                columns: new[] { "tenant_id", "effective_date" });

            migrationBuilder.CreateIndex(
                name: "ix_price_adjustments_tenant_id_organizational_unit_id",
                schema: "controle_ativos",
                table: "price_adjustments",
                columns: new[] { "tenant_id", "organizational_unit_id" });

            migrationBuilder.CreateIndex(
                name: "ix_price_adjustments_tenant_id_target_type_target_id",
                schema: "controle_ativos",
                table: "price_adjustments",
                columns: new[] { "tenant_id", "target_type", "target_id" });

            migrationBuilder.CreateIndex(
                name: "ix_suppliers_tenant_id_document_number",
                schema: "controle_ativos",
                table: "suppliers",
                columns: new[] { "tenant_id", "document_number" },
                unique: true,
                filter: "document_number IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_suppliers_tenant_id_name",
                schema: "controle_ativos",
                table: "suppliers",
                columns: new[] { "tenant_id", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_tenants_slug",
                schema: "controle_ativos",
                table: "tenants",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_timeline_events_correlation_id",
                schema: "controle_ativos",
                table: "timeline_events",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "ix_timeline_events_entity_occurred",
                schema: "controle_ativos",
                table: "timeline_events",
                columns: new[] { "tenant_id", "entity_type", "entity_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_timeline_events_tenant_id_event_type_occurred_at",
                schema: "controle_ativos",
                table: "timeline_events",
                columns: new[] { "tenant_id", "event_type", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_timeline_events_tenant_id_occurred_at",
                schema: "controle_ativos",
                table: "timeline_events",
                columns: new[] { "tenant_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_timeline_events_tenant_id_organizational_unit_id_occurred_at",
                schema: "controle_ativos",
                table: "timeline_events",
                columns: new[] { "tenant_id", "organizational_unit_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_users_primary_organizational_unit_id",
                schema: "controle_ativos",
                table: "users",
                column: "primary_organizational_unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_tenant_id_email",
                schema: "controle_ativos",
                table: "users",
                columns: new[] { "tenant_id", "email" });

            migrationBuilder.CreateIndex(
                name: "ix_users_tenant_id_external_subject",
                schema: "controle_ativos",
                table: "users",
                columns: new[] { "tenant_id", "external_subject" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alerts",
                schema: "controle_ativos");

            migrationBuilder.DropTable(
                name: "audit_logs",
                schema: "controle_ativos");

            migrationBuilder.DropTable(
                name: "cost_entries",
                schema: "controle_ativos");

            migrationBuilder.DropTable(
                name: "documents",
                schema: "controle_ativos");

            migrationBuilder.DropTable(
                name: "management_assignments",
                schema: "controle_ativos");

            migrationBuilder.DropTable(
                name: "price_adjustments",
                schema: "controle_ativos");

            migrationBuilder.DropTable(
                name: "tenants",
                schema: "controle_ativos");

            migrationBuilder.DropTable(
                name: "timeline_events",
                schema: "controle_ativos");

            migrationBuilder.DropTable(
                name: "assets",
                schema: "controle_ativos");

            migrationBuilder.DropTable(
                name: "contracts",
                schema: "controle_ativos");

            migrationBuilder.DropTable(
                name: "cost_centers",
                schema: "controle_ativos");

            migrationBuilder.DropTable(
                name: "suppliers",
                schema: "controle_ativos");

            migrationBuilder.DropTable(
                name: "users",
                schema: "controle_ativos");

            migrationBuilder.DropTable(
                name: "organizational_units",
                schema: "controle_ativos");
        }
    }
}
