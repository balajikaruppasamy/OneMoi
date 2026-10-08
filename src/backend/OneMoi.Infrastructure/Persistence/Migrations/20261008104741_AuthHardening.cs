using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneMoi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AuthHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "failed_login_count",
                schema: "auth",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "lockout_until",
                schema: "auth",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "password_changed_at",
                schema: "auth",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "security_stamp",
                schema: "auth",
                table: "users",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "family_id",
                schema: "auth",
                table: "refresh_tokens",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "replaced_by_hash",
                schema: "auth",
                table: "refresh_tokens",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "revoked_reason",
                schema: "auth",
                table: "refresh_tokens",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "failed_pin_count",
                schema: "core",
                table: "operators",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "lockout_until",
                schema: "core",
                table: "operators",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "security_stamp",
                schema: "core",
                table: "operators",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_family_id",
                schema: "auth",
                table: "refresh_tokens",
                column: "family_id");

            // Give every existing login its own random security stamp and put old refresh tokens in their own family
            migrationBuilder.Sql("UPDATE auth.users SET security_stamp = md5(random()::text || id::text) WHERE security_stamp = '';");
            migrationBuilder.Sql("UPDATE core.operators SET security_stamp = md5(random()::text || id::text) WHERE security_stamp = '';");
            migrationBuilder.Sql("UPDATE auth.refresh_tokens SET family_id = md5(id::text) WHERE family_id = '';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_refresh_tokens_family_id",
                schema: "auth",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "failed_login_count",
                schema: "auth",
                table: "users");

            migrationBuilder.DropColumn(
                name: "lockout_until",
                schema: "auth",
                table: "users");

            migrationBuilder.DropColumn(
                name: "password_changed_at",
                schema: "auth",
                table: "users");

            migrationBuilder.DropColumn(
                name: "security_stamp",
                schema: "auth",
                table: "users");

            migrationBuilder.DropColumn(
                name: "family_id",
                schema: "auth",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "replaced_by_hash",
                schema: "auth",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "revoked_reason",
                schema: "auth",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "failed_pin_count",
                schema: "core",
                table: "operators");

            migrationBuilder.DropColumn(
                name: "lockout_until",
                schema: "core",
                table: "operators");

            migrationBuilder.DropColumn(
                name: "security_stamp",
                schema: "core",
                table: "operators");
        }
    }
}
