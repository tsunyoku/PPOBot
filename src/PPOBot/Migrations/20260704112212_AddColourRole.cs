using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PPOBot.Migrations
{
    /// <inheritdoc />
    public partial class AddColourRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "colour_roles",
                columns: table => new
                {
                    role_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    colour = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_colour_roles", x => x.role_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "colour_roles");
        }
    }
}
