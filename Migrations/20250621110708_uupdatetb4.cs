using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kfrpj.Migrations
{
    /// <inheritdoc />
    public partial class uupdatetb4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_room_tenant_rel_sys_users_user_id",
                table: "room_tenant_rel");

            migrationBuilder.DropColumn(
                name: "water_units",
                table: "water_meters_list");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "room_tenant_rel",
                newName: "tenant_id");

            migrationBuilder.RenameIndex(
                name: "IX_room_tenant_rel_user_id",
                table: "room_tenant_rel",
                newName: "IX_room_tenant_rel_tenant_id");

            migrationBuilder.RenameColumn(
                name: "paid_date",
                table: "room_charges_list",
                newName: "payment_date");

            migrationBuilder.AddForeignKey(
                name: "FK_room_tenant_rel_tenants_list_tenant_id",
                table: "room_tenant_rel",
                column: "tenant_id",
                principalTable: "tenants_list",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_room_tenant_rel_tenants_list_tenant_id",
                table: "room_tenant_rel");

            migrationBuilder.RenameColumn(
                name: "tenant_id",
                table: "room_tenant_rel",
                newName: "user_id");

            migrationBuilder.RenameIndex(
                name: "IX_room_tenant_rel_tenant_id",
                table: "room_tenant_rel",
                newName: "IX_room_tenant_rel_user_id");

            migrationBuilder.RenameColumn(
                name: "payment_date",
                table: "room_charges_list",
                newName: "paid_date");

            migrationBuilder.AddColumn<int>(
                name: "water_units",
                table: "water_meters_list",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_room_tenant_rel_sys_users_user_id",
                table: "room_tenant_rel",
                column: "user_id",
                principalTable: "sys_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
