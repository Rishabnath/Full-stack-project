using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace foodsupply.api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRoleApplications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_role_applications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    applicant_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Pending"),
                    business_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    application_details = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: false),
                    review_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    submitted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_role_applications", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_role_applications_app_users_applicant_user_id",
                        column: x => x.applicant_user_id,
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_role_applications_app_users_reviewed_by_user_id",
                        column: x => x.reviewed_by_user_id,
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_role_applications_applicant_user_id",
                table: "user_role_applications",
                column: "applicant_user_id",
                unique: true,
                filter: "\"status\" = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_user_role_applications_reviewed_by_user_id",
                table: "user_role_applications",
                column: "reviewed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_role_applications_status_submitted_at_utc",
                table: "user_role_applications",
                columns: new[] { "status", "submitted_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_role_applications");
        }
    }
}
