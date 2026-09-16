using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelHup.INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class init7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RatePlansversions_RatePlans_RatePlanId",
                table: "RatePlansversions");

            migrationBuilder.DropIndex(
                name: "IX_RatePlansversions_RatePlanId",
                table: "RatePlansversions");

            migrationBuilder.AlterColumn<string>(
                name: "Rules",
                table: "RatePlansversions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_RatePlansversions_RatePlanId_VersionNumber",
                table: "RatePlansversions",
                columns: new[] { "RatePlanId", "VersionNumber" },
                unique: true)
                .Annotation("SqlServer:Clustered", false);

            migrationBuilder.AddForeignKey(
                name: "FK_RatePlansversions_RatePlans_RatePlanId",
                table: "RatePlansversions",
                column: "RatePlanId",
                principalTable: "RatePlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RatePlansversions_RatePlans_RatePlanId",
                table: "RatePlansversions");

            migrationBuilder.DropIndex(
                name: "IX_RatePlansversions_RatePlanId_VersionNumber",
                table: "RatePlansversions");

            migrationBuilder.AlterColumn<string>(
                name: "Rules",
                table: "RatePlansversions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000);

            migrationBuilder.CreateIndex(
                name: "IX_RatePlansversions_RatePlanId",
                table: "RatePlansversions",
                column: "RatePlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_RatePlansversions_RatePlans_RatePlanId",
                table: "RatePlansversions",
                column: "RatePlanId",
                principalTable: "RatePlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
