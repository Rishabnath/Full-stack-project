using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace foodsupply.api.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueDefaultAddressPerUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ux_customer_addresses_user_id_default",
                table: "customer_addresses",
                column: "user_id",
                unique: true,
                filter: "\"is_default\" = TRUE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_customer_addresses_user_id_default",
                table: "customer_addresses");

        }
    }
}
