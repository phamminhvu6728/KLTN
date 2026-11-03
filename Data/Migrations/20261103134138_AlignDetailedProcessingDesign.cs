using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TuTuc.Data.Migrations
{
    /// <inheritdoc />
    public partial class AlignDetailedProcessingDesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Budget",
                table: "Trips",
                type: "decimal(18,0)",
                precision: 18,
                scale: 0,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,0)",
                oldPrecision: 18);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "After",
                table: "AuditEntries",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Before",
                table: "AuditEntries",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EventCode",
                table: "AuditEntries",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<decimal>(
                name: "Cost",
                table: "Activities",
                type: "decimal(18,0)",
                precision: 18,
                scale: 0,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,0)",
                oldPrecision: 18);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "After",
                table: "AuditEntries");

            migrationBuilder.DropColumn(
                name: "Before",
                table: "AuditEntries");

            migrationBuilder.DropColumn(
                name: "EventCode",
                table: "AuditEntries");

            migrationBuilder.AlterColumn<decimal>(
                name: "Budget",
                table: "Trips",
                type: "decimal(18,0)",
                precision: 18,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,0)",
                oldPrecision: 18,
                oldScale: 0,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Cost",
                table: "Activities",
                type: "decimal(18,0)",
                precision: 18,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,0)",
                oldPrecision: 18,
                oldScale: 0,
                oldNullable: true);
        }
    }
}
