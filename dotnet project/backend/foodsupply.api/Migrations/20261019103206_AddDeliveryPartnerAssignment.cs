using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace foodsupply.api.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryPartnerAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "delivery_partner_id",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_orders_delivery_partner_id_status",
                table: "orders",
                columns: new[] { "delivery_partner_id", "status" });

            migrationBuilder.AddForeignKey(
                name: "FK_orders_app_users_delivery_partner_id",
                table: "orders",
                column: "delivery_partner_id",
                principalTable: "app_users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_orders_app_users_delivery_partner_id",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_delivery_partner_id_status",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "delivery_partner_id",
                table: "orders");
        }
    }
}
