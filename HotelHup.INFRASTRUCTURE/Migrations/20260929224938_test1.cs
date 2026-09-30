using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelHup.INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class test1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RatePlans_Properties_PropertyId",
                table: "RatePlans");

            migrationBuilder.DropForeignKey(
                name: "FK_RatePlansversions_RatePlans_RatePlanId",
                table: "RatePlansversions");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_RatePlans_RatePlanId",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Taxes_Code",
                table: "Taxes");

            migrationBuilder.DropIndex(
                name: "IX_Taxes_Name",
                table: "Taxes");

            migrationBuilder.DropIndex(
                name: "IX_Taxes_PropertyId",
                table: "Taxes");

            migrationBuilder.DropIndex(
                name: "IX_RoomTypes_Name",
                table: "RoomTypes");

            migrationBuilder.DropIndex(
                name: "IX_RoomTypes_property_id",
                table: "RoomTypes");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_property_id",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_RoomNumber",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_ReservationRooms_ReservationId",
                table: "ReservationRooms");

            migrationBuilder.DropIndex(
                name: "IX_ReservationRooms_RoomId",
                table: "ReservationRooms");

            migrationBuilder.DropIndex(
                name: "IX_RatePlansversions_RatePlanId_VersionNumber",
                table: "RatePlansversions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RatePlans",
                table: "RatePlans");

            migrationBuilder.DropIndex(
                name: "IX_RatePlans_PropertyId",
                table: "RatePlans");

            migrationBuilder.DropIndex(
                name: "IX_RatePlans_RoomTypeId_Name",
                table: "RatePlans");

            migrationBuilder.DropColumn(
                name: "CancellationPolicySnapshot",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "RateSnapshot",
                table: "Reservations");

            migrationBuilder.RenameColumn(
                name: "TaxAmount",
                table: "Reservations",
                newName: "TotalTaxAmount");

            migrationBuilder.RenameColumn(
                name: "RatePlanId",
                table: "Reservations",
                newName: "DepositPolicyVersionId");

            migrationBuilder.RenameColumn(
                name: "FeeAmount",
                table: "Reservations",
                newName: "TotalFeeAmount");

            migrationBuilder.RenameColumn(
                name: "DiscountAmount",
                table: "Reservations",
                newName: "TotalDiscountAmount");

            migrationBuilder.RenameIndex(
                name: "IX_Reservations_RatePlanId",
                table: "Reservations",
                newName: "IX_Reservations_DepositPolicyVersionId");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "ReservationRooms",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "RateSnapshot",
                table: "ReservationRooms",
                newName: "AssignmentReason");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Rooms",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseAmount",
                table: "Reservations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "DepositPolicyId",
                table: "Reservations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Reservations",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AssignedAt",
                table: "ReservationRooms",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<decimal>(
                name: "BaseAmount",
                table: "ReservationRooms",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckInDate",
                table: "ReservationRooms",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckOutDate",
                table: "ReservationRooms",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "ReservationRooms",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FeeAmount",
                table: "ReservationRooms",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsCurrent",
                table: "ReservationRooms",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Nights",
                table: "ReservationRooms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RatePlanId",
                table: "ReservationRooms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RatePlanVersionId",
                table: "ReservationRooms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReleasedAt",
                table: "ReservationRooms",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ReservationRooms",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AlterColumn<string>(
                name: "Rules",
                table: "RatePlansversions",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<string>(
                name: "Rules",
                table: "RatePlans",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Guests",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "propertyid",
                table: "Guests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "CreditBalance",
                table: "Folios",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Folios",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "ServiceNameSnapshot",
                table: "FolioItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_RatePlans",
                table: "RatePlans",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "ReservationCancellationPolicySnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReservationId = table.Column<int>(type: "int", nullable: false),
                    CancellationPolicyId = table.Column<int>(type: "int", nullable: true),
                    CancellationPolicyVersionId = table.Column<int>(type: "int", nullable: true),
                    PolicyName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Rules = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    FreeCancellationHours = table.Column<int>(type: "int", nullable: false),
                    CancellationFeePercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    FixedCancellationFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsNonRefundable = table.Column<bool>(type: "bit", nullable: false),
                    CutoffHours = table.Column<int>(type: "int", nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CancellationPolicyId1 = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationCancellationPolicySnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReservationCancellationPolicySnapshots_CancellationPolicies_CancellationPolicyId",
                        column: x => x.CancellationPolicyId,
                        principalTable: "CancellationPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservationCancellationPolicySnapshots_CancellationPolicies_CancellationPolicyId1",
                        column: x => x.CancellationPolicyId1,
                        principalTable: "CancellationPolicies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReservationCancellationPolicySnapshots_Reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "Reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReservationCancellationPolicySnapshots_cancellationPolicyVersions_CancellationPolicyVersionId",
                        column: x => x.CancellationPolicyVersionId,
                        principalTable: "cancellationPolicyVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReservationDepositPolicySnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReservationId = table.Column<int>(type: "int", nullable: false),
                    DepositPolicyId = table.Column<int>(type: "int", nullable: true),
                    DepositPolicyVersionId = table.Column<int>(type: "int", nullable: true),
                    PolicyName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Percentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    RequiredDepositAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationDepositPolicySnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReservationDepositPolicySnapshots_DepositPolicies_DepositPolicyId",
                        column: x => x.DepositPolicyId,
                        principalTable: "DepositPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservationDepositPolicySnapshots_Reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "Reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReservationDepositPolicySnapshots_depositPolicyVersions_DepositPolicyVersionId",
                        column: x => x.DepositPolicyVersionId,
                        principalTable: "depositPolicyVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReservationRoomRatePlanSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReservationRoomId = table.Column<int>(type: "int", nullable: false),
                    RatePlanId = table.Column<int>(type: "int", nullable: false),
                    RatePlanVersionId = table.Column<int>(type: "int", nullable: false),
                    RatePlanName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    RatePlanType = table.Column<int>(type: "int", nullable: false),
                    IsRefundable = table.Column<bool>(type: "bit", nullable: false),
                    RulesSnapshot = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    NightlyRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BaseAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FeeAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationRoomRatePlanSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReservationRoomRatePlanSnapshots_RatePlans_RatePlanId",
                        column: x => x.RatePlanId,
                        principalTable: "RatePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservationRoomRatePlanSnapshots_RatePlansversions_RatePlanVersionId",
                        column: x => x.RatePlanVersionId,
                        principalTable: "RatePlansversions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservationRoomRatePlanSnapshots_ReservationRooms_ReservationRoomId",
                        column: x => x.ReservationRoomId,
                        principalTable: "ReservationRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReservationStatusHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReservationId = table.Column<int>(type: "int", nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: true),
                    ToStatus = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ActorId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReservationStatusHistories_Reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "Reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Taxes_PropertyId_Code",
                table: "Taxes",
                columns: new[] { "PropertyId", "Code" },
                unique: true)
                .Annotation("SqlServer:Clustered", false);

            migrationBuilder.CreateIndex(
                name: "IX_Taxes_PropertyId_Name",
                table: "Taxes",
                columns: new[] { "PropertyId", "Name" },
                unique: true)
                .Annotation("SqlServer:Clustered", false);

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypes_property_id_Name",
                table: "RoomTypes",
                columns: new[] { "property_id", "Name" },
                unique: true)
                .Annotation("SqlServer:Clustered", false);

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_property_id_RoomNumber",
                table: "Rooms",
                columns: new[] { "property_id", "RoomNumber" },
                unique: true)
                .Annotation("SqlServer:Clustered", false);

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_DepositPolicyId",
                table: "Reservations",
                column: "DepositPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_PropertyId_CheckInDate_CheckOutDate",
                table: "Reservations",
                columns: new[] { "PropertyId", "CheckInDate", "CheckOutDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ReservationRooms_RatePlanId",
                table: "ReservationRooms",
                column: "RatePlanId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationRooms_RatePlanVersionId",
                table: "ReservationRooms",
                column: "RatePlanVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationRooms_ReservationId_IsCurrent",
                table: "ReservationRooms",
                columns: new[] { "ReservationId", "IsCurrent" },
                filter: "[IsCurrent] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationRooms_ReservationId_RoomId",
                table: "ReservationRooms",
                columns: new[] { "ReservationId", "RoomId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReservationRooms_RoomId_IsCurrent",
                table: "ReservationRooms",
                columns: new[] { "RoomId", "IsCurrent" },
                filter: "[IsCurrent] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_RatePlansversions_RatePlanId_VersionNumber",
                table: "RatePlansversions",
                columns: new[] { "RatePlanId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RatePlans_PropertyId_Name",
                table: "RatePlans",
                columns: new[] { "PropertyId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RatePlans_RoomTypeId",
                table: "RatePlans",
                column: "RoomTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Guests_propertyid",
                table: "Guests",
                column: "propertyid");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationCancellationPolicySnapshots_CancellationPolicyId",
                table: "ReservationCancellationPolicySnapshots",
                column: "CancellationPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationCancellationPolicySnapshots_CancellationPolicyId1",
                table: "ReservationCancellationPolicySnapshots",
                column: "CancellationPolicyId1");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationCancellationPolicySnapshots_CancellationPolicyVersionId",
                table: "ReservationCancellationPolicySnapshots",
                column: "CancellationPolicyVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationCancellationPolicySnapshots_ReservationId",
                table: "ReservationCancellationPolicySnapshots",
                column: "ReservationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReservationDepositPolicySnapshots_DepositPolicyId",
                table: "ReservationDepositPolicySnapshots",
                column: "DepositPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationDepositPolicySnapshots_DepositPolicyVersionId",
                table: "ReservationDepositPolicySnapshots",
                column: "DepositPolicyVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationDepositPolicySnapshots_ReservationId",
                table: "ReservationDepositPolicySnapshots",
                column: "ReservationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReservationRoomRatePlanSnapshots_RatePlanId",
                table: "ReservationRoomRatePlanSnapshots",
                column: "RatePlanId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationRoomRatePlanSnapshots_RatePlanVersionId",
                table: "ReservationRoomRatePlanSnapshots",
                column: "RatePlanVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationRoomRatePlanSnapshots_ReservationRoomId",
                table: "ReservationRoomRatePlanSnapshots",
                column: "ReservationRoomId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReservationStatusHistories_ReservationId",
                table: "ReservationStatusHistories",
                column: "ReservationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Guests_Properties_propertyid",
                table: "Guests",
                column: "propertyid",
                principalTable: "Properties",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RatePlans_Properties_PropertyId",
                table: "RatePlans",
                column: "PropertyId",
                principalTable: "Properties",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RatePlansversions_RatePlans_RatePlanId",
                table: "RatePlansversions",
                column: "RatePlanId",
                principalTable: "RatePlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReservationRooms_RatePlans_RatePlanId",
                table: "ReservationRooms",
                column: "RatePlanId",
                principalTable: "RatePlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReservationRooms_RatePlansversions_RatePlanVersionId",
                table: "ReservationRooms",
                column: "RatePlanVersionId",
                principalTable: "RatePlansversions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_DepositPolicies_DepositPolicyId",
                table: "Reservations",
                column: "DepositPolicyId",
                principalTable: "DepositPolicies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_depositPolicyVersions_DepositPolicyVersionId",
                table: "Reservations",
                column: "DepositPolicyVersionId",
                principalTable: "depositPolicyVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Guests_Properties_propertyid",
                table: "Guests");

            migrationBuilder.DropForeignKey(
                name: "FK_RatePlans_Properties_PropertyId",
                table: "RatePlans");

            migrationBuilder.DropForeignKey(
                name: "FK_RatePlansversions_RatePlans_RatePlanId",
                table: "RatePlansversions");

            migrationBuilder.DropForeignKey(
                name: "FK_ReservationRooms_RatePlans_RatePlanId",
                table: "ReservationRooms");

            migrationBuilder.DropForeignKey(
                name: "FK_ReservationRooms_RatePlansversions_RatePlanVersionId",
                table: "ReservationRooms");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_DepositPolicies_DepositPolicyId",
                table: "Reservations");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_depositPolicyVersions_DepositPolicyVersionId",
                table: "Reservations");

            migrationBuilder.DropTable(
                name: "ReservationCancellationPolicySnapshots");

            migrationBuilder.DropTable(
                name: "ReservationDepositPolicySnapshots");

            migrationBuilder.DropTable(
                name: "ReservationRoomRatePlanSnapshots");

            migrationBuilder.DropTable(
                name: "ReservationStatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_Taxes_PropertyId_Code",
                table: "Taxes");

            migrationBuilder.DropIndex(
                name: "IX_Taxes_PropertyId_Name",
                table: "Taxes");

            migrationBuilder.DropIndex(
                name: "IX_RoomTypes_property_id_Name",
                table: "RoomTypes");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_property_id_RoomNumber",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_DepositPolicyId",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_PropertyId_CheckInDate_CheckOutDate",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_ReservationRooms_RatePlanId",
                table: "ReservationRooms");

            migrationBuilder.DropIndex(
                name: "IX_ReservationRooms_RatePlanVersionId",
                table: "ReservationRooms");

            migrationBuilder.DropIndex(
                name: "IX_ReservationRooms_ReservationId_IsCurrent",
                table: "ReservationRooms");

            migrationBuilder.DropIndex(
                name: "IX_ReservationRooms_ReservationId_RoomId",
                table: "ReservationRooms");

            migrationBuilder.DropIndex(
                name: "IX_ReservationRooms_RoomId_IsCurrent",
                table: "ReservationRooms");

            migrationBuilder.DropIndex(
                name: "IX_RatePlansversions_RatePlanId_VersionNumber",
                table: "RatePlansversions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RatePlans",
                table: "RatePlans");

            migrationBuilder.DropIndex(
                name: "IX_RatePlans_PropertyId_Name",
                table: "RatePlans");

            migrationBuilder.DropIndex(
                name: "IX_RatePlans_RoomTypeId",
                table: "RatePlans");

            migrationBuilder.DropIndex(
                name: "IX_Guests_propertyid",
                table: "Guests");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "BaseAmount",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "DepositPolicyId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "AssignedAt",
                table: "ReservationRooms");

            migrationBuilder.DropColumn(
                name: "BaseAmount",
                table: "ReservationRooms");

            migrationBuilder.DropColumn(
                name: "CheckInDate",
                table: "ReservationRooms");

            migrationBuilder.DropColumn(
                name: "CheckOutDate",
                table: "ReservationRooms");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "ReservationRooms");

            migrationBuilder.DropColumn(
                name: "FeeAmount",
                table: "ReservationRooms");

            migrationBuilder.DropColumn(
                name: "IsCurrent",
                table: "ReservationRooms");

            migrationBuilder.DropColumn(
                name: "Nights",
                table: "ReservationRooms");

            migrationBuilder.DropColumn(
                name: "RatePlanId",
                table: "ReservationRooms");

            migrationBuilder.DropColumn(
                name: "RatePlanVersionId",
                table: "ReservationRooms");

            migrationBuilder.DropColumn(
                name: "ReleasedAt",
                table: "ReservationRooms");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ReservationRooms");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Guests");

            migrationBuilder.DropColumn(
                name: "propertyid",
                table: "Guests");

            migrationBuilder.DropColumn(
                name: "CreditBalance",
                table: "Folios");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Folios");

            migrationBuilder.DropColumn(
                name: "ServiceNameSnapshot",
                table: "FolioItems");

            migrationBuilder.RenameColumn(
                name: "TotalTaxAmount",
                table: "Reservations",
                newName: "TaxAmount");

            migrationBuilder.RenameColumn(
                name: "TotalFeeAmount",
                table: "Reservations",
                newName: "FeeAmount");

            migrationBuilder.RenameColumn(
                name: "TotalDiscountAmount",
                table: "Reservations",
                newName: "DiscountAmount");

            migrationBuilder.RenameColumn(
                name: "DepositPolicyVersionId",
                table: "Reservations",
                newName: "RatePlanId");

            migrationBuilder.RenameIndex(
                name: "IX_Reservations_DepositPolicyVersionId",
                table: "Reservations",
                newName: "IX_Reservations_RatePlanId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "ReservationRooms",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "AssignmentReason",
                table: "ReservationRooms",
                newName: "RateSnapshot");

            migrationBuilder.AddColumn<string>(
                name: "CancellationPolicySnapshot",
                table: "Reservations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RateSnapshot",
                table: "Reservations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Rules",
                table: "RatePlansversions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AlterColumn<string>(
                name: "Rules",
                table: "RatePlans",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AddPrimaryKey(
                name: "PK_RatePlans",
                table: "RatePlans",
                column: "Id")
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "IX_Taxes_Code",
                table: "Taxes",
                column: "Code",
                unique: true)
                .Annotation("SqlServer:Clustered", false);

            migrationBuilder.CreateIndex(
                name: "IX_Taxes_Name",
                table: "Taxes",
                column: "Name",
                unique: true)
                .Annotation("SqlServer:Clustered", false);

            migrationBuilder.CreateIndex(
                name: "IX_Taxes_PropertyId",
                table: "Taxes",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypes_Name",
                table: "RoomTypes",
                column: "Name",
                unique: true)
                .Annotation("SqlServer:Clustered", false);

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypes_property_id",
                table: "RoomTypes",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_property_id",
                table: "Rooms",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_RoomNumber",
                table: "Rooms",
                column: "RoomNumber",
                unique: true)
                .Annotation("SqlServer:Clustered", false);

            migrationBuilder.CreateIndex(
                name: "IX_ReservationRooms_ReservationId",
                table: "ReservationRooms",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationRooms_RoomId",
                table: "ReservationRooms",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_RatePlansversions_RatePlanId_VersionNumber",
                table: "RatePlansversions",
                columns: new[] { "RatePlanId", "VersionNumber" },
                unique: true)
                .Annotation("SqlServer:Clustered", false);

            migrationBuilder.CreateIndex(
                name: "IX_RatePlans_PropertyId",
                table: "RatePlans",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_RatePlans_RoomTypeId_Name",
                table: "RatePlans",
                columns: new[] { "RoomTypeId", "Name" },
                unique: true)
                .Annotation("SqlServer:Clustered", false);

            migrationBuilder.AddForeignKey(
                name: "FK_RatePlans_Properties_PropertyId",
                table: "RatePlans",
                column: "PropertyId",
                principalTable: "Properties",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RatePlansversions_RatePlans_RatePlanId",
                table: "RatePlansversions",
                column: "RatePlanId",
                principalTable: "RatePlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_RatePlans_RatePlanId",
                table: "Reservations",
                column: "RatePlanId",
                principalTable: "RatePlans",
                principalColumn: "Id");
        }
    }
}
