using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kfrpj.Migrations
{
    /// <inheritdoc />
    public partial class CreateFinanceTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "electric_meters_list",
                columns: table => new
                {
                    electric_meter_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    room_id = table.Column<int>(type: "int", nullable: false),
                    meter_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    old_meter = table.Column<int>(type: "int", nullable: false),
                    new_meter = table.Column<int>(type: "int", nullable: false),
                    electric_units = table.Column<int>(type: "int", nullable: false),
                    electric_bill = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    is_paid = table.Column<bool>(type: "bit", nullable: false),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    record_status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_electric_meters_list", x => x.electric_meter_id);
                    table.ForeignKey(
                        name: "FK_electric_meters_list_rooms_list_room_id",
                        column: x => x.room_id,
                        principalTable: "rooms_list",
                        principalColumn: "room_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "room_charges_list",
                columns: table => new
                {
                    room_charge_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    room_id = table.Column<int>(type: "int", nullable: false),
                    charge_month = table.Column<DateTime>(type: "datetime2", nullable: false),
                    room_price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    due_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_paid = table.Column<bool>(type: "bit", nullable: false),
                    paid_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    record_status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_charges_list", x => x.room_charge_id);
                    table.ForeignKey(
                        name: "FK_room_charges_list_rooms_list_room_id",
                        column: x => x.room_id,
                        principalTable: "rooms_list",
                        principalColumn: "room_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "water_meters_list",
                columns: table => new
                {
                    water_meter_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    room_id = table.Column<int>(type: "int", nullable: false),
                    meter_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    old_meter = table.Column<int>(type: "int", nullable: false),
                    new_meter = table.Column<int>(type: "int", nullable: false),
                    water_units = table.Column<int>(type: "int", nullable: false),
                    water_bill = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    is_paid = table.Column<bool>(type: "bit", nullable: false),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    record_status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_water_meters_list", x => x.water_meter_id);
                    table.ForeignKey(
                        name: "FK_water_meters_list_rooms_list_room_id",
                        column: x => x.room_id,
                        principalTable: "rooms_list",
                        principalColumn: "room_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_electric_meters_list_room_id",
                table: "electric_meters_list",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "IX_room_charges_list_room_id",
                table: "room_charges_list",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "IX_water_meters_list_room_id",
                table: "water_meters_list",
                column: "room_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "electric_meters_list");

            migrationBuilder.DropTable(
                name: "room_charges_list");

            migrationBuilder.DropTable(
                name: "water_meters_list");
        }
    }
}
