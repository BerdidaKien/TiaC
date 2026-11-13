using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASI.Basecode.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMustCompleteSetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "must_complete_setup",
                table: "user",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "must_complete_setup",
                table: "user");
        }
    }
}
