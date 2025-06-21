using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kfrpj.Migrations
{
    /// <inheritdoc />
    public partial class CreateFinanceTables22 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "new_meter",
                table: "water_meters_list");

            migrationBuilder.RenameColumn(
                name: "old_meter",
                table: "water_meters_list",
                newName: "people_count");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "people_count",
                table: "water_meters_list",
                newName: "old_meter");

            migrationBuilder.AddColumn<int>(
                name: "new_meter",
                table: "water_meters_list",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
