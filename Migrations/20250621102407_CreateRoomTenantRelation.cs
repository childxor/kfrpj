using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kfrpj.Migrations
{
    /// <inheritdoc />
    public partial class CreateRoomTenantRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "room_tenant_rel",
                columns: table => new
                {
                    rel_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    room_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    start_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    end_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    record_status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_tenant_rel", x => x.rel_id);
                    table.ForeignKey(
                        name: "FK_room_tenant_rel_rooms_list_room_id",
                        column: x => x.room_id,
                        principalTable: "rooms_list",
                        principalColumn: "room_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_room_tenant_rel_sys_users_user_id",
                        column: x => x.user_id,
                        principalTable: "sys_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_room_tenant_rel_room_id",
                table: "room_tenant_rel",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "IX_room_tenant_rel_user_id",
                table: "room_tenant_rel",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "room_tenant_rel");
        }
    }
}
