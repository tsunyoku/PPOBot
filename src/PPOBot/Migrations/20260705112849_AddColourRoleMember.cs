using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPOBot.Migrations
{
    /// <inheritdoc />
    public partial class AddColourRoleMember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "colour_role_members",
                columns: table => new
                {
                    user_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    role_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_colour_role_members", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_colour_role_members_colour_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "colour_roles",
                        principalColumn: "role_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_colour_role_members_role_id",
                table: "colour_role_members",
                column: "role_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "colour_role_members");
        }
    }
}
