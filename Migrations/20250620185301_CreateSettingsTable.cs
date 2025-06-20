using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kfrpj.Migrations
{
    /// <inheritdoc />
    public partial class CreateSettingsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "settings_list",
                columns: table => new
                {
                    setting_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    setting_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    setting_key = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    setting_value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    data_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    unit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    category = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    record_status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_settings_list", x => x.setting_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "settings_list");
        }
    }
}
