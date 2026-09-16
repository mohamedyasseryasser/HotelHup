using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelHup.INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class init6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Price",
                table: "RatePlans");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "RatePlans",
                newName: "Id");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "RoomTypes",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "RatePlanId",
                table: "Reservations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PropertyId",
                table: "RatePlans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "RatePlans",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "RatePlansversions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RatePlanId = table.Column<int>(type: "int", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Rules = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRefundable = table.Column<bool>(type: "bit", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RatePlansversions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RatePlansversions_RatePlans_RatePlanId",
                        column: x => x.RatePlanId,
                        principalTable: "RatePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_RatePlanId",
                table: "Reservations",
                column: "RatePlanId");

            migrationBuilder.CreateIndex(
                name: "IX_RatePlans_PropertyId",
                table: "RatePlans",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_RatePlansversions_RatePlanId",
                table: "RatePlansversions",
                column: "RatePlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_RatePlans_Properties_PropertyId",
                table: "RatePlans",
                column: "PropertyId",
                principalTable: "Properties",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_RatePlans_RatePlanId",
                table: "Reservations",
                column: "RatePlanId",
                principalTable: "RatePlans",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RatePlans_Properties_PropertyId",
                table: "RatePlans");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_RatePlans_RatePlanId",
                table: "Reservations");

            migrationBuilder.DropTable(
                name: "RatePlansversions");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_RatePlanId",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_RatePlans_PropertyId",
                table: "RatePlans");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "RoomTypes");

            migrationBuilder.DropColumn(
                name: "RatePlanId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "PropertyId",
                table: "RatePlans");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "RatePlans");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "RatePlans",
                newName: "id");

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "RatePlans",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
