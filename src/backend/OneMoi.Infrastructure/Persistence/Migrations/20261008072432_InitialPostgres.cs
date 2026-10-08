using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OneMoi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "auth");

            migrationBuilder.EnsureSchema(
                name: "evt");

            migrationBuilder.EnsureSchema(
                name: "master");

            migrationBuilder.EnsureSchema(
                name: "moi");

            migrationBuilder.EnsureSchema(
                name: "core");

            migrationBuilder.CreateTable(
                name: "audit_logs",
                schema: "auth",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<int>(type: "integer", nullable: true),
                    principal_type = table.Column<int>(type: "integer", nullable: true),
                    principal_id = table.Column<int>(type: "integer", nullable: true),
                    action = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    entity_name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    entity_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    details = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "denominations",
                schema: "master",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    value = table.Column<int>(type: "integer", nullable: false),
                    is_coin = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_denominations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notification_logs",
                schema: "auth",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<int>(type: "integer", nullable: true),
                    channel = table.Column<int>(type: "integer", nullable: false),
                    recipient = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "otp_requests",
                schema: "auth",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    channel = table.Column<int>(type: "integer", nullable: false),
                    destination = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    purpose = table.Column<int>(type: "integer", nullable: false),
                    code_hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    max_attempts = table.Column<int>(type: "integer", nullable: false),
                    is_used = table.Column<bool>(type: "boolean", nullable: false),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_otp_requests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "persons",
                schema: "moi",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    mobile = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    initial = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    name_ta = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    spouse_initial = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    spouse_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    spouse_name_ta = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    work = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    city = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    city_ta = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_persons", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "plans",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    price_monthly = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    price_per_function = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    max_operators = table.Column<int>(type: "integer", nullable: false),
                    max_functions_per_month = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plans", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                schema: "auth",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    principal_type = table.Column<int>(type: "integer", nullable: false),
                    principal_id = table.Column<int>(type: "integer", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_ip = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "auth",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    full_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    full_name_ta = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    mobile = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    password_hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    user_type = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_mobile_verified = table.Column<bool>(type: "boolean", nullable: false),
                    is_email_verified = table.Column<bool>(type: "boolean", nullable: false),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    person_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_persons_person_id",
                        column: x => x.person_id,
                        principalSchema: "moi",
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenants",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    name_ta = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    tagline = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    owner_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    mobile = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    alt_mobile = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    address_line = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    city = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    district = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    state = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    pincode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    gstin = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    logo_path = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    primary_color = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    receipt_header = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    receipt_footer = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    plan_id = table.Column<int>(type: "integer", nullable: true),
                    trial_ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenants", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenants_plans_plan_id",
                        column: x => x.plan_id,
                        principalSchema: "core",
                        principalTable: "plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "expense_categories",
                schema: "master",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<int>(type: "integer", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name_ta = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_expense_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_expense_categories_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "function_types",
                schema: "master",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<int>(type: "integer", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name_ta = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_function_types", x => x.id);
                    table.ForeignKey(
                        name: "fk_function_types_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "gift_item_types",
                schema: "master",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    unit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<int>(type: "integer", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name_ta = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gift_item_types", x => x.id);
                    table.ForeignKey(
                        name: "fk_gift_item_types_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "moi_categories",
                schema: "master",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    is_highlighted = table.Column<bool>(type: "boolean", nullable: false),
                    color = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<int>(type: "integer", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name_ta = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_moi_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_moi_categories_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "operators",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    name_ta = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    mobile = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    pin_hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_operators", x => x.id);
                    table.ForeignKey(
                        name: "fk_operators_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenant_members",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    role = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_members_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tenant_members_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "auth",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "functions",
                schema: "evt",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    function_type_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name_ta = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    owner_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    owner_name_ta = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    owner_mobile = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    owner_email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    location_ta = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    city = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    function_date = table.Column<DateTime>(type: "date", nullable: false),
                    start_time = table.Column<TimeSpan>(type: "time", nullable: true),
                    end_time = table.Column<TimeSpan>(type: "time", nullable: true),
                    expected_guests = table.Column<int>(type: "integer", nullable: true),
                    other_details = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    allow_cash = table.Column<bool>(type: "boolean", nullable: false),
                    allow_upi = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_functions", x => x.id);
                    table.ForeignKey(
                        name: "fk_functions_function_types_function_type_id",
                        column: x => x.function_type_id,
                        principalSchema: "master",
                        principalTable: "function_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_functions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "core",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "counters",
                schema: "evt",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    function_id = table.Column<int>(type: "integer", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_counters", x => x.id);
                    table.ForeignKey(
                        name: "fk_counters_functions_function_id",
                        column: x => x.function_id,
                        principalSchema: "evt",
                        principalTable: "functions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "function_expenses",
                schema: "moi",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    function_id = table.Column<int>(type: "integer", nullable: false),
                    expense_category_id = table.Column<int>(type: "integer", nullable: true),
                    taken_by_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    taken_by_name_ta = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    relation = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    taken_by_mobile = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    purpose = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    payment_mode = table.Column<int>(type: "integer", nullable: false),
                    entry_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    operator_id = table.Column<int>(type: "integer", nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_function_expenses", x => x.id);
                    table.ForeignKey(
                        name: "fk_function_expenses_expense_categories_expense_category_id",
                        column: x => x.expense_category_id,
                        principalSchema: "master",
                        principalTable: "expense_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_function_expenses_functions_function_id",
                        column: x => x.function_id,
                        principalSchema: "evt",
                        principalTable: "functions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "moi_entries",
                schema: "moi",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    function_id = table.Column<int>(type: "integer", nullable: false),
                    counter_id = table.Column<int>(type: "integer", nullable: true),
                    operator_id = table.Column<int>(type: "integer", nullable: true),
                    person_id = table.Column<int>(type: "integer", nullable: true),
                    serial_no = table.Column<int>(type: "integer", nullable: false),
                    receipt_no = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    mobile = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    initial = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    name_ta = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    spouse_initial = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    spouse_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    spouse_name_ta = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    work = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    city = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    city_ta = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    moi_category_id = table.Column<int>(type: "integer", nullable: true),
                    is_highlighted = table.Column<bool>(type: "boolean", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    payment_mode = table.Column<int>(type: "integer", nullable: false),
                    payment_ref = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    reversal_reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    entry_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    client_ref = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_moi_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_moi_entries_counters_counter_id",
                        column: x => x.counter_id,
                        principalSchema: "evt",
                        principalTable: "counters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_moi_entries_functions_function_id",
                        column: x => x.function_id,
                        principalSchema: "evt",
                        principalTable: "functions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_moi_entries_moi_categories_moi_category_id",
                        column: x => x.moi_category_id,
                        principalSchema: "master",
                        principalTable: "moi_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_moi_entries_operators_operator_id",
                        column: x => x.operator_id,
                        principalSchema: "core",
                        principalTable: "operators",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_moi_entries_persons_person_id",
                        column: x => x.person_id,
                        principalSchema: "moi",
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "operator_assignments",
                schema: "evt",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<int>(type: "integer", nullable: false),
                    function_id = table.Column<int>(type: "integer", nullable: false),
                    counter_id = table.Column<int>(type: "integer", nullable: false),
                    operator_id = table.Column<int>(type: "integer", nullable: false),
                    valid_from = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    valid_to = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_operator_assignments", x => x.id);
                    table.ForeignKey(
                        name: "fk_operator_assignments_counters_counter_id",
                        column: x => x.counter_id,
                        principalSchema: "evt",
                        principalTable: "counters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_operator_assignments_functions_function_id",
                        column: x => x.function_id,
                        principalSchema: "evt",
                        principalTable: "functions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_operator_assignments_operators_operator_id",
                        column: x => x.operator_id,
                        principalSchema: "core",
                        principalTable: "operators",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "moi_entry_denominations",
                schema: "moi",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    moi_entry_id = table.Column<int>(type: "integer", nullable: false),
                    note_value = table.Column<int>(type: "integer", nullable: false),
                    count = table.Column<int>(type: "integer", nullable: false),
                    total = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_moi_entry_denominations", x => x.id);
                    table.ForeignKey(
                        name: "fk_moi_entry_denominations_moi_entries_moi_entry_id",
                        column: x => x.moi_entry_id,
                        principalSchema: "moi",
                        principalTable: "moi_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "moi_entry_gifts",
                schema: "moi",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    moi_entry_id = table.Column<int>(type: "integer", nullable: false),
                    gift_item_type_id = table.Column<int>(type: "integer", nullable: true),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description_ta = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    estimated_value = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_moi_entry_gifts", x => x.id);
                    table.ForeignKey(
                        name: "fk_moi_entry_gifts_gift_item_types_gift_item_type_id",
                        column: x => x.gift_item_type_id,
                        principalSchema: "master",
                        principalTable: "gift_item_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_moi_entry_gifts_moi_entries_moi_entry_id",
                        column: x => x.moi_entry_id,
                        principalSchema: "moi",
                        principalTable: "moi_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_tenant_id_created_at",
                schema: "auth",
                table: "audit_logs",
                columns: new[] { "tenant_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_counters_function_id_number",
                schema: "evt",
                table: "counters",
                columns: new[] { "function_id", "number" });

            migrationBuilder.CreateIndex(
                name: "ix_denominations_value",
                schema: "master",
                table: "denominations",
                column: "value",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_expense_categories_tenant_id_sort_order",
                schema: "master",
                table: "expense_categories",
                columns: new[] { "tenant_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_function_expenses_expense_category_id",
                schema: "moi",
                table: "function_expenses",
                column: "expense_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_function_expenses_function_id",
                schema: "moi",
                table: "function_expenses",
                column: "function_id");

            migrationBuilder.CreateIndex(
                name: "ix_function_types_tenant_id_sort_order",
                schema: "master",
                table: "function_types",
                columns: new[] { "tenant_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_functions_code",
                schema: "evt",
                table: "functions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_functions_function_type_id",
                schema: "evt",
                table: "functions",
                column: "function_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_functions_owner_mobile",
                schema: "evt",
                table: "functions",
                column: "owner_mobile");

            migrationBuilder.CreateIndex(
                name: "ix_functions_tenant_id_function_date",
                schema: "evt",
                table: "functions",
                columns: new[] { "tenant_id", "function_date" });

            migrationBuilder.CreateIndex(
                name: "ix_gift_item_types_tenant_id_sort_order",
                schema: "master",
                table: "gift_item_types",
                columns: new[] { "tenant_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_moi_categories_tenant_id_sort_order",
                schema: "master",
                table: "moi_categories",
                columns: new[] { "tenant_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_moi_entries_client_ref",
                schema: "moi",
                table: "moi_entries",
                column: "client_ref",
                unique: true,
                filter: "client_ref IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_moi_entries_counter_id",
                schema: "moi",
                table: "moi_entries",
                column: "counter_id");

            migrationBuilder.CreateIndex(
                name: "ix_moi_entries_function_id_serial_no",
                schema: "moi",
                table: "moi_entries",
                columns: new[] { "function_id", "serial_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_moi_entries_moi_category_id",
                schema: "moi",
                table: "moi_entries",
                column: "moi_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_moi_entries_name_city",
                schema: "moi",
                table: "moi_entries",
                columns: new[] { "name", "city" });

            migrationBuilder.CreateIndex(
                name: "ix_moi_entries_operator_id",
                schema: "moi",
                table: "moi_entries",
                column: "operator_id");

            migrationBuilder.CreateIndex(
                name: "ix_moi_entries_person_id",
                schema: "moi",
                table: "moi_entries",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "ix_moi_entries_tenant_id_mobile",
                schema: "moi",
                table: "moi_entries",
                columns: new[] { "tenant_id", "mobile" });

            migrationBuilder.CreateIndex(
                name: "ix_moi_entry_denominations_moi_entry_id",
                schema: "moi",
                table: "moi_entry_denominations",
                column: "moi_entry_id");

            migrationBuilder.CreateIndex(
                name: "ix_moi_entry_gifts_gift_item_type_id",
                schema: "moi",
                table: "moi_entry_gifts",
                column: "gift_item_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_moi_entry_gifts_moi_entry_id",
                schema: "moi",
                table: "moi_entry_gifts",
                column: "moi_entry_id");

            migrationBuilder.CreateIndex(
                name: "ix_operator_assignments_counter_id",
                schema: "evt",
                table: "operator_assignments",
                column: "counter_id");

            migrationBuilder.CreateIndex(
                name: "ix_operator_assignments_function_id",
                schema: "evt",
                table: "operator_assignments",
                column: "function_id");

            migrationBuilder.CreateIndex(
                name: "ix_operator_assignments_operator_id_is_active",
                schema: "evt",
                table: "operator_assignments",
                columns: new[] { "operator_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_operators_tenant_id_code",
                schema: "core",
                table: "operators",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_otp_requests_destination_purpose_created_at",
                schema: "auth",
                table: "otp_requests",
                columns: new[] { "destination", "purpose", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_persons_mobile",
                schema: "moi",
                table: "persons",
                column: "mobile",
                unique: true,
                filter: "mobile IS NOT NULL AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_principal_type_principal_id",
                schema: "auth",
                table: "refresh_tokens",
                columns: new[] { "principal_type", "principal_id" });

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token_hash",
                schema: "auth",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_members_tenant_id_user_id",
                schema: "core",
                table: "tenant_members",
                columns: new[] { "tenant_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_members_user_id",
                schema: "core",
                table: "tenant_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenants_code",
                schema: "core",
                table: "tenants",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenants_plan_id",
                schema: "core",
                table: "tenants",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                schema: "auth",
                table: "users",
                column: "email",
                unique: true,
                filter: "email IS NOT NULL AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_users_mobile_user_type",
                schema: "auth",
                table: "users",
                columns: new[] { "mobile", "user_type" },
                unique: true,
                filter: "mobile IS NOT NULL AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_users_person_id",
                schema: "auth",
                table: "users",
                column: "person_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "denominations",
                schema: "master");

            migrationBuilder.DropTable(
                name: "function_expenses",
                schema: "moi");

            migrationBuilder.DropTable(
                name: "moi_entry_denominations",
                schema: "moi");

            migrationBuilder.DropTable(
                name: "moi_entry_gifts",
                schema: "moi");

            migrationBuilder.DropTable(
                name: "notification_logs",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "operator_assignments",
                schema: "evt");

            migrationBuilder.DropTable(
                name: "otp_requests",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "refresh_tokens",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "tenant_members",
                schema: "core");

            migrationBuilder.DropTable(
                name: "expense_categories",
                schema: "master");

            migrationBuilder.DropTable(
                name: "gift_item_types",
                schema: "master");

            migrationBuilder.DropTable(
                name: "moi_entries",
                schema: "moi");

            migrationBuilder.DropTable(
                name: "users",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "counters",
                schema: "evt");

            migrationBuilder.DropTable(
                name: "moi_categories",
                schema: "master");

            migrationBuilder.DropTable(
                name: "operators",
                schema: "core");

            migrationBuilder.DropTable(
                name: "persons",
                schema: "moi");

            migrationBuilder.DropTable(
                name: "functions",
                schema: "evt");

            migrationBuilder.DropTable(
                name: "function_types",
                schema: "master");

            migrationBuilder.DropTable(
                name: "tenants",
                schema: "core");

            migrationBuilder.DropTable(
                name: "plans",
                schema: "core");
        }
    }
}
