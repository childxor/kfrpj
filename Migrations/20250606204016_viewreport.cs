using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kfrpj.Migrations
{
    /// <inheritdoc />
    public partial class viewreport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tenant_report_view_models",
                columns: table => new
                {
                    TotalCount = table.Column<int>(type: "int", nullable: false),
                    GeneratedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GeneratedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tenant_report_view_models");
        }
    }
}
