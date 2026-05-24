using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Laundry.Migrations
{
    /// <inheritdoc />
    public partial class TrackDriverAssignmentTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DriverAssignedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "PromoCodes",
                keyColumn: "PromoId",
                keyValue: 1,
                column: "ExpiryDate",
                value: new DateTime(2026, 6, 24, 3, 57, 58, 693, DateTimeKind.Local).AddTicks(3089));

            migrationBuilder.UpdateData(
                table: "PromoCodes",
                keyColumn: "PromoId",
                keyValue: 2,
                column: "ExpiryDate",
                value: new DateTime(2026, 7, 24, 3, 57, 58, 693, DateTimeKind.Local).AddTicks(3101));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 5, 24, 3, 57, 58, 693, DateTimeKind.Local).AddTicks(2111), "$2a$11$4TnFMLVmGfcDmyXrMRaCJuugmIYp.UmxFcnSF8fkaSZjBazprom9W" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DriverAssignedAt",
                table: "Orders");

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
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 5, 24, 1, 39, 3, 462, DateTimeKind.Local).AddTicks(4397), "$2a$11$u4bTBq/dqY1QNKz8RVMNVeKCnv7DwIgekcTfe/f.RaYV6O3CuQsOy" });
        }
    }
}
