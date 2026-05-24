using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Laundry.Migrations
{
    /// <inheritdoc />
    public partial class seeding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdentityNumber",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SystemUsername",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleRegistrationPlate",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "PromoCodes",
                keyColumn: "PromoId",
                keyValue: 1,
                column: "ExpiryDate",
                value: new DateTime(2026, 6, 24, 1, 39, 3, 462, DateTimeKind.Local).AddTicks(5178));

            migrationBuilder.UpdateData(
                table: "PromoCodes",
                keyColumn: "PromoId",
                keyValue: 2,
                column: "ExpiryDate",
                value: new DateTime(2026, 7, 24, 1, 39, 3, 462, DateTimeKind.Local).AddTicks(5190));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                columns: new[] { "CreatedAt", "IdentityNumber", "PasswordHash", "SystemUsername", "VehicleRegistrationPlate" },
                values: new object[] { new DateTime(2026, 5, 24, 1, 39, 3, 462, DateTimeKind.Local).AddTicks(4397), null, "$2a$11$u4bTBq/dqY1QNKz8RVMNVeKCnv7DwIgekcTfe/f.RaYV6O3CuQsOy", null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdentityNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SystemUsername",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "VehicleRegistrationPlate",
                table: "Users");

            migrationBuilder.UpdateData(
                table: "PromoCodes",
                keyColumn: "PromoId",
                keyValue: 1,
                column: "ExpiryDate",
                value: new DateTime(2026, 6, 24, 0, 0, 17, 185, DateTimeKind.Local).AddTicks(6534));

            migrationBuilder.UpdateData(
                table: "PromoCodes",
                keyColumn: "PromoId",
                keyValue: 2,
                column: "ExpiryDate",
                value: new DateTime(2026, 7, 24, 0, 0, 17, 185, DateTimeKind.Local).AddTicks(6547));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 5, 24, 0, 0, 17, 185, DateTimeKind.Local).AddTicks(5624), "$2a$11$IMPMVNE9hjHSjl3/XDR59O3.Ekod5sN1DGsRQYsuCRDbGQR02AuUy" });
        }
    }
}
